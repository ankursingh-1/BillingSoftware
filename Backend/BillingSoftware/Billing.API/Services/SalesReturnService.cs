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
/// Sales returns (credit notes). A return is always made against the original invoice:
/// each line can only be returned up to what was sold minus what was already returned,
/// stock goes back in, and any refund is capped by what the customer has actually paid.
/// The sale is locked for the duration so two returns (or a return and a payment void)
/// cannot see the same figures.
/// </summary>
public class SalesReturnService
{
    private const int MaxPageSize = 100;

    private readonly BillingDbContext _context;
    private readonly IInventoryService _inventory;
    private readonly IDocumentNumberService _numbers;
    private readonly IAuditService _audit;
    private readonly ICurrentUser _user;

    public SalesReturnService(
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

    public async Task<SalesReturnResponse> CreateAsync(CreateSalesReturnRequest request)
    {
        var reason = request.Reason?.Trim();

        if (string.IsNullOrEmpty(reason) || reason.Length < 3)
            throw new BusinessException("Enter a reason for the return.");

        if (GstCalculator.Round(request.RefundAmount) != request.RefundAmount)
            throw new BusinessException("The refund can have at most 2 decimal places.");

        if (request.RefundAmount > 0 && request.RefundMethod == null)
            throw new BusinessException("Select how the refund is paid.");

        // One entry per invoice line.
        var requested = request.Items
            .GroupBy(i => i.SaleItemId)
            .ToDictionary(g => g.Key, g => g.Sum(i => i.Quantity));

        await using var transaction = await _context.Database.BeginTransactionAsync();

        await SqlTransactionHelper.LockSaleAsync(_context, request.SaleId);

        var sale = await _context.Sales
            .Include(s => s.SaleItems)
            .Include(s => s.Payments)
            .Include(s => s.Customer)
            .FirstOrDefaultAsync(s => s.Id == request.SaleId)
            ?? throw BusinessException.NotFound("Invoice not found.");

        var previousItems = await _context.SalesReturnItems
            .AsNoTracking()
            .Where(r => r.SalesReturn.SaleId == sale.Id)
            .ToListAsync();

        var previousReturns = await _context.SalesReturns
            .AsNoTracking()
            .Where(r => r.SaleId == sale.Id)
            .Select(r => new { r.TotalAmount, r.RefundAmount })
            .ToListAsync();

        // ---- Validate every line before touching anything ----

        var problems = new List<string>();
        var lineAmounts = new List<(SaleItem Item, int Quantity, ReturnLineAmounts Amounts, decimal Rate)>();

        foreach (var (itemId, quantity) in requested)
        {
            var item = sale.SaleItems.FirstOrDefault(i => i.Id == itemId);

            if (item == null)
            {
                problems.Add($"Line {itemId} does not belong to invoice {sale.InvoiceNumber}.");
                continue;
            }

            var earlier = previousItems.Where(r => r.SaleItemId == item.Id).ToList();
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
                item.SellingPrice,
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
                ReturnCalculator.Line(original, soFar, quantity, sale.IsInterState), rate));
        }

        if (problems.Count > 0)
            throw BusinessException.Conflict(string.Join(" ", problems));

        // Does this return finish the whole invoice? Then it also carries the round-off.
        var completes = sale.SaleItems.All(item =>
        {
            var returnedBefore = previousItems.Where(r => r.SaleItemId == item.Id).Sum(r => r.Quantity);
            var now = requested.TryGetValue(item.Id, out var q) ? q : 0;
            return returnedBefore + now == item.Quantity;
        });

        var totalAmount = ReturnCalculator.DocumentTotal(
            lineAmounts.Select(l => l.Amounts), completes, sale.RoundOff);

        // ---- Refund: only what the customer has really paid in excess ----

        var paid = sale.Payments.Where(p => !p.IsVoided).Sum(p => p.Amount);
        var returnedBefore = previousReturns.Sum(r => r.TotalAmount);
        var refundedBefore = previousReturns.Sum(r => r.RefundAmount);

        var after = PaymentCalculator.Summarise(
            sale.TotalAmount, returnedBefore + totalAmount, paid, refundedBefore, sale.PaymentTracked);

        if (request.RefundAmount > 0)
        {
            if (!sale.PaymentTracked)
            {
                throw BusinessException.Conflict(
                    "Payments were not recorded for this older invoice, so a refund cannot be checked. Record the original payment first, or return the goods without a refund.");
            }

            if (request.RefundAmount > after.RefundDue)
            {
                throw BusinessException.Conflict(
                    $"The refund ({request.RefundAmount:0.00}) is more than the customer has paid for the returned goods ({after.RefundDue:0.00}).");
            }
        }

        // ---- Write ----

        var now = DateTime.Now;
        int sequence;

        try
        {
            sequence = await _numbers.NextAsync(
                DocumentNumbering.SeriesKey(DocumentNumbering.SalesReturnPrefix, now));
        }
        catch (ArgumentOutOfRangeException)
        {
            throw BusinessException.Conflict("The credit note series for this financial year is exhausted.");
        }

        var number = DocumentNumbering.Format(DocumentNumbering.SalesReturnPrefix, now, sequence);

        var salesReturn = new SalesReturn
        {
            ReturnNumber = number,
            SaleId = sale.Id,
            ReturnDate = now,
            Reason = reason,
            Subtotal = lineAmounts.Sum(l => l.Amounts.Taxable),
            CgstTotal = lineAmounts.Sum(l => l.Amounts.Tax.Cgst),
            SgstTotal = lineAmounts.Sum(l => l.Amounts.Tax.Sgst),
            IgstTotal = lineAmounts.Sum(l => l.Amounts.Tax.Igst),
            TaxTotal = lineAmounts.Sum(l => l.Amounts.Tax.Total),
            TotalAmount = totalAmount,
            RefundAmount = request.RefundAmount,
            RefundMethod = request.RefundAmount > 0 ? request.RefundMethod : null,
            CreatedOn = DateTime.UtcNow,
            CreatedBy = _user.Email
        };

        foreach (var (item, quantity, amounts, rate) in lineAmounts)
        {
            salesReturn.Items.Add(new SalesReturnItem
            {
                SaleItemId = item.Id,
                ProductId = item.ProductId,
                Quantity = quantity,
                UnitPrice = item.SellingPrice,
                TaxableAmount = amounts.Taxable,
                TaxRate = rate,
                CgstAmount = amounts.Tax.Cgst,
                SgstAmount = amounts.Tax.Sgst,
                IgstAmount = amounts.Tax.Igst,
                TaxAmount = amounts.Tax.Total,
                Total = amounts.Total
            });
        }

        _context.SalesReturns.Add(salesReturn);

        foreach (var line in lineAmounts.OrderBy(l => l.Item.ProductId))
        {
            await _inventory.AdjustStockAsync(
                line.Item.ProductId, line.Quantity, StockTransactionType.SalesReturn,
                number, $"Sales return against {sale.InvoiceNumber}");
        }

        await _context.SaveChangesAsync();

        _audit.Record(
            "SalesReturn.Create", "SalesReturn", salesReturn.Id.ToString(),
            $"{number} against {sale.InvoiceNumber}; value {totalAmount:0.00}; refund {request.RefundAmount:0.00}");

        await _context.SaveChangesAsync();
        await transaction.CommitAsync();

        return new SalesReturnResponse
        {
            ReturnId = salesReturn.Id,
            ReturnNumber = number,
            TotalAmount = totalAmount,
            RefundAmount = request.RefundAmount,
            RefundDue = Math.Max(0, after.RefundDue - request.RefundAmount),
            Outstanding = sale.PaymentTracked ? after.Outstanding : 0
        };
    }

    /// <summary>Pays out (part of) the money still owed to the customer after returns.</summary>
    public async Task<SalesReturnResponse> RecordRefundAsync(int returnId, RecordRefundRequest request)
    {
        if (GstCalculator.Round(request.Amount) != request.Amount)
            throw new BusinessException("The refund can have at most 2 decimal places.");

        await using var transaction = await _context.Database.BeginTransactionAsync();

        var saleId = await _context.SalesReturns
            .Where(r => r.Id == returnId)
            .Select(r => (int?)r.SaleId)
            .FirstOrDefaultAsync()
            ?? throw BusinessException.NotFound("Return not found.");

        await SqlTransactionHelper.LockSaleAsync(_context, saleId);

        var salesReturn = await _context.SalesReturns.FirstAsync(r => r.Id == returnId);

        var sale = await _context.Sales
            .AsNoTracking()
            .Include(s => s.Payments)
            .FirstAsync(s => s.Id == saleId);

        if (!sale.PaymentTracked)
        {
            throw BusinessException.Conflict(
                "Payments were not recorded for this older invoice, so a refund cannot be checked.");
        }

        var returns = await _context.SalesReturns
            .AsNoTracking()
            .Where(r => r.SaleId == saleId)
            .Select(r => new { r.TotalAmount, r.RefundAmount })
            .ToListAsync();

        var summary = PaymentCalculator.Summarise(
            sale.TotalAmount,
            returns.Sum(r => r.TotalAmount),
            sale.Payments.Where(p => !p.IsVoided).Sum(p => p.Amount),
            returns.Sum(r => r.RefundAmount),
            true);

        if (request.Amount > summary.RefundDue)
        {
            throw BusinessException.Conflict(
                summary.RefundDue <= 0
                    ? "Nothing is owed to the customer on this invoice."
                    : $"The refund ({request.Amount:0.00}) is more than the {summary.RefundDue:0.00} owed to the customer.");
        }

        salesReturn.RefundAmount += request.Amount;
        salesReturn.RefundMethod = request.Method;
        salesReturn.modifieson = DateTime.UtcNow;
        salesReturn.ModifiedBy = _user.Email;

        _audit.Record(
            "SalesReturn.Refund", "SalesReturn", salesReturn.Id.ToString(),
            $"{salesReturn.ReturnNumber}; refund {request.Amount:0.00} via {request.Method}");

        await _context.SaveChangesAsync();
        await transaction.CommitAsync();

        return new SalesReturnResponse
        {
            ReturnId = salesReturn.Id,
            ReturnNumber = salesReturn.ReturnNumber,
            TotalAmount = salesReturn.TotalAmount,
            RefundAmount = salesReturn.RefundAmount,
            RefundDue = summary.RefundDue - request.Amount,
            Outstanding = summary.Outstanding
        };
    }

    // ------------------------------------------------------------------
    // Read
    // ------------------------------------------------------------------

    public async Task<PagedResult<SalesReturnListItemDto>> SearchAsync(SalesReturnSearchRequest request)
    {
        var pageNumber = Math.Max(request.PageNumber, 1);
        var pageSize = Math.Clamp(request.PageSize, 1, MaxPageSize);

        var query = _context.SalesReturns.AsNoTracking();

        if (request.SaleId.HasValue)
            query = query.Where(r => r.SaleId == request.SaleId.Value);

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var search = request.Search.Trim();

            query = query.Where(r =>
                r.ReturnNumber.Contains(search) ||
                r.Sale.InvoiceNumber.Contains(search) ||
                r.Sale.Customer.Name.Contains(search));
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
            .Select(r => new SalesReturnListItemDto
            {
                Id = r.Id,
                ReturnNumber = r.ReturnNumber,
                ReturnDate = r.ReturnDate,
                SaleId = r.SaleId,
                InvoiceNumber = r.Sale.InvoiceNumber,
                CustomerName = r.Sale.Customer.Name,
                TotalAmount = r.TotalAmount,
                RefundAmount = r.RefundAmount
            })
            .ToListAsync();

        return new PagedResult<SalesReturnListItemDto>
        {
            TotalRecords = totalRecords,
            TotalPages = (int)Math.Ceiling((double)totalRecords / pageSize),
            PageNumber = pageNumber,
            PageSize = pageSize,
            Data = data
        };
    }

    public async Task<SalesReturnDetailDto> GetByIdAsync(int id)
    {
        var r = await _context.SalesReturns
            .AsNoTracking()
            .Include(x => x.Sale).ThenInclude(s => s.Customer)
            .Include(x => x.Items).ThenInclude(i => i.Product)
            .Include(x => x.Items).ThenInclude(i => i.SaleItem)
            .FirstOrDefaultAsync(x => x.Id == id)
            ?? throw BusinessException.NotFound("Return not found.");

        var siblings = await _context.SalesReturns
            .AsNoTracking()
            .Where(x => x.SaleId == r.SaleId)
            .Select(x => new { x.TotalAmount, x.RefundAmount })
            .ToListAsync();

        var paid = await _context.Payments
            .AsNoTracking()
            .Where(p => p.SaleId == r.SaleId && !p.IsVoided)
            .SumAsync(p => (decimal?)p.Amount) ?? 0;

        var summary = PaymentCalculator.Summarise(
            r.Sale.TotalAmount, siblings.Sum(x => x.TotalAmount), paid,
            siblings.Sum(x => x.RefundAmount), r.Sale.PaymentTracked);

        return new SalesReturnDetailDto
        {
            Id = r.Id,
            ReturnNumber = r.ReturnNumber,
            ReturnDate = r.ReturnDate,
            Reason = r.Reason,
            CreatedBy = r.CreatedBy,
            SaleId = r.SaleId,
            InvoiceNumber = r.Sale.InvoiceNumber,
            SaleDate = r.Sale.SaleDate,
            CustomerName = r.Sale.Customer.Name,
            CustomerGstin = r.Sale.CustomerGstin,
            IsInterState = r.Sale.IsInterState,
            Subtotal = r.Subtotal,
            CgstTotal = r.CgstTotal,
            SgstTotal = r.SgstTotal,
            IgstTotal = r.IgstTotal,
            TaxTotal = r.TaxTotal,
            RoundOff = r.TotalAmount - r.Subtotal - r.TaxTotal,
            TotalAmount = r.TotalAmount,
            RefundAmount = r.RefundAmount,
            RefundMethod = r.RefundMethod?.ToString(),
            RefundDue = r.Sale.PaymentTracked ? summary.RefundDue : 0,
            Items = r.Items.OrderBy(i => i.Id).Select(i => new ReturnItemDetailDto
            {
                Id = i.Id,
                ProductId = i.ProductId,
                ProductName = i.Product.Name,
                Sku = i.Product.SKU,
                HsnCode = i.SaleItem.HsnCode,
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