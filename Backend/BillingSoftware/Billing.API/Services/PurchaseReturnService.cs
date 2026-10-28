using Billing.API.Models;
using Billing.Application.Accounting;
using Billing.Application.DTOs.Returns;
using Billing.Application.Interfaces;
using Billing.Domain.Entities;
using Billing.Domain.Enums;
using Billing.Persistence.Context;
using Billing.Shared.Exceptions;
using Microsoft.EntityFrameworkCore;

namespace Billing.API.Services;

/// <summary>
/// Purchase returns (debit notes): goods sent back to the supplier. Quantities are
/// capped by what was bought minus what was already returned, and stock goes down
/// (a 409 if those units have already been sold). The purchase is locked meanwhile.
/// </summary>
public class PurchaseReturnService
{
    private const int MaxPageSize = 100;

    private readonly BillingDbContext _context;
    private readonly IInventoryService _inventory;
    private readonly IDocumentNumberService _numbers;
    private readonly IAuditService _audit;
    private readonly ICurrentUser _user;

    public PurchaseReturnService(
        BillingDbContext context,
        IInventoryService inventory,
        IDocumentNumberService numbers,
        IAuditService audit,
        ICurrentUser user)
    {
        _context = context;
        _inventory = inventory;
        _numbers = numbers;
        _audit = audit;
        _user = user;
    }

    public async Task<PurchaseReturnResponse> CreateAsync(CreatePurchaseReturnRequest request)
    {
        var reason = request.Reason?.Trim();

        if (string.IsNullOrEmpty(reason) || reason.Length < 3)
            throw new BusinessException("Enter a reason for the return.");

        if (GstCalculator.Round(request.RefundReceived) != request.RefundReceived)
            throw new BusinessException("The refund can have at most 2 decimal places.");

        if (request.RefundReceived > 0 && request.RefundMethod == null)
            throw new BusinessException("Select how the supplier paid the refund.");

        var requested = request.Items
            .GroupBy(i => i.PurchaseItemId)
            .ToDictionary(g => g.Key, g => g.Sum(i => i.Quantity));

        await using var transaction = await _context.Database.BeginTransactionAsync();

        await SqlTransactionHelper.LockPurchaseAsync(_context, request.PurchaseId);

        var purchase = await _context.Purchases
            .Include(p => p.PurchaseItems)
            .Include(p => p.Supplier)
            .FirstOrDefaultAsync(p => p.Id == request.PurchaseId)
            ?? throw BusinessException.NotFound("Purchase not found.");

        var previousItems = await _context.PurchaseReturnItems
            .AsNoTracking()
            .Where(r => r.PurchaseReturn.PurchaseId == purchase.Id)
            .ToListAsync();

        var problems = new List<string>();
        var lineAmounts = new List<(PurchaseItem Item, int Quantity, ReturnLineAmounts Amounts, decimal Rate)>();

        foreach (var (itemId, quantity) in requested)
        {
            var item = purchase.PurchaseItems.FirstOrDefault(i => i.Id == itemId);

            if (item == null)
            {
                problems.Add($"Line {itemId} does not belong to purchase {purchase.PurchaseNumber}.");
                continue;
            }

            var earlier = previousItems.Where(r => r.PurchaseItemId == item.Id).ToList();
            var earlierQuantity = earlier.Sum(r => r.Quantity);
            var returnable = item.Quantity - earlierQuantity;

            if (quantity > returnable)
            {
                problems.Add(returnable <= 0
                    ? $"Line {itemId} has already been fully returned."
                    : $"Only {returnable} of line {itemId} can still be returned ({quantity} requested).");
                continue;
            }

            var rate = item.TaxRate ?? 0m;
            var original = new OriginalLine(
                item.Quantity,
                item.PurchasePrice,
                rate,
                item.TaxableAmount ?? item.Total,
                new TaxBreakup(item.CgstAmount ?? 0m, item.SgstAmount ?? 0m, item.IgstAmount ?? 0m));

            var soFar = new ReturnedSoFar(
                earlierQuantity,
                earlier.Sum(r => r.TaxableAmount),
                new TaxBreakup(
                    earlier.Sum(r => r.CgstAmount),
                    earlier.Sum(r => r.SgstAmount),
                    earlier.Sum(r => r.IgstAmount)));

            lineAmounts.Add((item, quantity,
                ReturnCalculator.Line(original, soFar, quantity, purchase.IsInterState), rate));
        }

        if (problems.Count > 0)
            throw BusinessException.Conflict(string.Join(" ", problems));

        var completes = purchase.PurchaseItems.All(item =>
        {
            var before = previousItems.Where(r => r.PurchaseItemId == item.Id).Sum(r => r.Quantity);
            var now = requested.TryGetValue(item.Id, out var q) ? q : 0;
            return before + now == item.Quantity;
        });

        var totalAmount = ReturnCalculator.DocumentTotal(
            lineAmounts.Select(l => l.Amounts), completes, purchase.RoundOff);

        if (request.RefundReceived > totalAmount)
        {
            throw BusinessException.Conflict(
                $"The refund ({request.RefundReceived:0.00}) is more than the value of the goods returned ({totalAmount:0.00}).");
        }

        var now = DateTime.Now;
        int sequence;

        try
        {
            sequence = await _numbers.NextAsync(
                DocumentNumbering.SeriesKey(DocumentNumbering.PurchaseReturnPrefix, now));
        }
        catch (ArgumentOutOfRangeException)
        {
            throw BusinessException.Conflict("The debit note series for this financial year is exhausted.");
        }

        var number = DocumentNumbering.Format(DocumentNumbering.PurchaseReturnPrefix, now, sequence);

        var purchaseReturn = new PurchaseReturn
        {
            ReturnNumber = number,
            PurchaseId = purchase.Id,
            ReturnDate = now,
            Reason = reason,
            Subtotal = lineAmounts.Sum(l => l.Amounts.Taxable),
            CgstTotal = lineAmounts.Sum(l => l.Amounts.Tax.Cgst),
            SgstTotal = lineAmounts.Sum(l => l.Amounts.Tax.Sgst),
            IgstTotal = lineAmounts.Sum(l => l.Amounts.Tax.Igst),
            TaxTotal = lineAmounts.Sum(l => l.Amounts.Tax.Total),
            TotalAmount = totalAmount,
            RefundReceived = request.RefundReceived,
            RefundMethod = request.RefundReceived > 0 ? request.RefundMethod : null,
            AdjustmentNote = string.IsNullOrWhiteSpace(request.AdjustmentNote) ? null : request.AdjustmentNote.Trim(),
            CreatedOn = DateTime.UtcNow,
            CreatedBy = _user.Email
        };

        foreach (var (item, quantity, amounts, rate) in lineAmounts)
        {
            purchaseReturn.Items.Add(new PurchaseReturnItem
            {
                PurchaseItemId = item.Id,
                ProductId = item.ProductId,
                Quantity = quantity,
                UnitPrice = item.PurchasePrice,
                TaxableAmount = amounts.Taxable,
                TaxRate = rate,
                CgstAmount = amounts.Tax.Cgst,
                SgstAmount = amounts.Tax.Sgst,
                IgstAmount = amounts.Tax.Igst,
                TaxAmount = amounts.Tax.Total,
                Total = amounts.Total
            });
        }

        _context.PurchaseReturns.Add(purchaseReturn);

        // Stock out; a 409 if those units are no longer on the shelf.
        foreach (var line in lineAmounts.OrderBy(l => l.Item.ProductId))
        {
            await _inventory.AdjustStockAsync(
                line.Item.ProductId, line.Quantity, StockTransactionType.PurchaseReturn,
                number, $"Purchase return against {purchase.PurchaseNumber}");
        }

        await _context.SaveChangesAsync();

        _audit.Record(
            "PurchaseReturn.Create", "PurchaseReturn", purchaseReturn.Id.ToString(),
            $"{number} against {purchase.PurchaseNumber}; value {totalAmount:0.00}; refund {request.RefundReceived:0.00}");

        await _context.SaveChangesAsync();
        await transaction.CommitAsync();

        return new PurchaseReturnResponse
        {
            ReturnId = purchaseReturn.Id,
            ReturnNumber = number,
            TotalAmount = totalAmount,
            RefundReceived = request.RefundReceived
        };
    }

    public async Task<PagedResult<PurchaseReturnListItemDto>> SearchAsync(PurchaseReturnSearchRequest request)
    {
        var pageNumber = Math.Max(request.PageNumber, 1);
        var pageSize = Math.Clamp(request.PageSize, 1, MaxPageSize);

        var query = _context.PurchaseReturns.AsNoTracking();

        if (request.PurchaseId.HasValue)
            query = query.Where(r => r.PurchaseId == request.PurchaseId.Value);

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var search = request.Search.Trim();

            query = query.Where(r =>
                r.ReturnNumber.Contains(search) ||
                r.Purchase.PurchaseNumber.Contains(search) ||
                r.Purchase.Supplier.Name.Contains(search) ||
                r.Purchase.Supplier.CompanyName.Contains(search));
        }

        if (request.FromDate.HasValue)
        {
            var from = request.FromDate.Value.Date;
            query = query.Where(r => r.ReturnDate >= from);
        }

        if (request.ToDate.HasValue)
        {
            var to = request.ToDate.Value.Date.AddDays(1);
            query = query.Where(r => r.ReturnDate < to);
        }

        int totalRecords = await query.CountAsync();

        var data = await query
            .OrderByDescending(r => r.ReturnDate)
            .ThenByDescending(r => r.Id)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .Select(r => new PurchaseReturnListItemDto
            {
                Id = r.Id,
                ReturnNumber = r.ReturnNumber,
                ReturnDate = r.ReturnDate,
                PurchaseId = r.PurchaseId,
                PurchaseNumber = r.Purchase.PurchaseNumber,
                SupplierName = r.Purchase.Supplier.CompanyName != ""
                    ? r.Purchase.Supplier.CompanyName
                    : r.Purchase.Supplier.Name,
                TotalAmount = r.TotalAmount,
                RefundReceived = r.RefundReceived
            })
            .ToListAsync();

        return new PagedResult<PurchaseReturnListItemDto>
        {
            TotalRecords = totalRecords,
            TotalPages = (int)Math.Ceiling((double)totalRecords / pageSize),
            PageNumber = pageNumber,
            PageSize = pageSize,
            Data = data
        };
    }

    public async Task<PurchaseReturnDetailDto> GetByIdAsync(int id)
    {
        var r = await _context.PurchaseReturns
            .AsNoTracking()
            .Include(x => x.Purchase).ThenInclude(p => p.Supplier)
            .Include(x => x.Items).ThenInclude(i => i.Product)
            .Include(x => x.Items).ThenInclude(i => i.PurchaseItem)
            .FirstOrDefaultAsync(x => x.Id == id)
            ?? throw BusinessException.NotFound("Return not found.");

        var supplier = r.Purchase.Supplier;

        return new PurchaseReturnDetailDto
        {
            Id = r.Id,
            ReturnNumber = r.ReturnNumber,
            ReturnDate = r.ReturnDate,
            Reason = r.Reason,
            CreatedBy = r.CreatedBy,
            PurchaseId = r.PurchaseId,
            PurchaseNumber = r.Purchase.PurchaseNumber,
            SupplierInvoiceNumber = r.Purchase.SupplierInvoiceNumber,
            SupplierName = supplier.CompanyName != "" ? supplier.CompanyName : supplier.Name,
            SupplierGstin = r.Purchase.SupplierGstin,
            IsInterState = r.Purchase.IsInterState,
            Subtotal = r.Subtotal,
            CgstTotal = r.CgstTotal,
            SgstTotal = r.SgstTotal,
            IgstTotal = r.IgstTotal,
            TaxTotal = r.TaxTotal,
            RoundOff = r.TotalAmount - r.Subtotal - r.TaxTotal,
            TotalAmount = r.TotalAmount,
            RefundReceived = r.RefundReceived,
            RefundMethod = r.RefundMethod?.ToString(),
            AdjustmentNote = r.AdjustmentNote,
            Items = r.Items.OrderBy(i => i.Id).Select(i => new ReturnItemDetailDto
            {
                Id = i.Id,
                ProductId = i.ProductId,
                ProductName = i.Product.Name,
                Sku = i.Product.SKU,
                HsnCode = i.PurchaseItem.HsnCode,
                Quantity = i.Quantity,
                UnitPrice = i.UnitPrice,
                TaxableAmount = i.TaxableAmount,
                TaxRate = i.TaxRate,
                CgstAmount = i.CgstAmount,
                SgstAmount = i.SgstAmount,
                IgstAmount = i.IgstAmount,
                TaxAmount = i.TaxAmount,
                Total = i.Total
            }).ToList()
        };
    }
}