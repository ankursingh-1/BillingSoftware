using Billing.API.Models;
using Billing.Application.Accounting;
using Billing.Application.DTOs.Purchases;
using Billing.Application.Interfaces;
using Billing.Domain.Entities;
using Billing.Domain.Enums;
using Billing.Persistence.Context;
using Billing.Shared.Exceptions;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace Billing.API.Services;

/// <summary>
/// Purchases (stock coming in). Same safety rules as sales: everything is validated
/// before anything is written, the number comes from the database sequence inside the
/// transaction, stock moves by an atomic UPDATE, and a client request id makes a retry
/// harmless. Input tax is split CGST+SGST or IGST by comparing the supplier's state
/// with the company's state.
/// </summary>
public class PurchaseService
{
    private const int MaxPageSize = 100;

    private readonly BillingDbContext _context;
    private readonly IInventoryService _inventoryService;
    private readonly IDocumentNumberService _numbers;
    private readonly IAuditService _audit;
    private readonly ICurrentUser _user;

    public PurchaseService(
        BillingDbContext context,
        IInventoryService inventoryService,
        IDocumentNumberService numbers,
        IAuditService audit,
        ICurrentUser user)
    {
        _context = context;
        _inventoryService = inventoryService;
        _numbers = numbers;
        _audit = audit;
        _user = user;
    }

    // ------------------------------------------------------------------
    // Create
    // ------------------------------------------------------------------

    public async Task<PurchaseResponse> CreateAsync(CreatePurchaseRequest request)
    {
        if (request.ClientRequestId is { } requestId)
        {
            var existing = await FindByRequestIdAsync(requestId);
            if (existing != null)
                return existing;
        }

        try
        {
            return await CreateCoreAsync(request);
        }
        catch (DbUpdateException ex) when (
            request.ClientRequestId.HasValue && IsUniqueViolation(ex))
        {
            var existing = await FindByRequestIdAsync(request.ClientRequestId.Value);
            if (existing != null)
                return existing;

            throw;
        }
    }

    private async Task<PurchaseResponse> CreateCoreAsync(CreatePurchaseRequest request)
    {
        var purchaseDate = request.PurchaseDate;

        if (purchaseDate.Year < 2000)
            throw new BusinessException("Enter a valid purchase date.");

        if (purchaseDate.Date > DateTime.Now.Date)
            throw new BusinessException("The purchase date cannot be in the future.");

        foreach (var item in request.Items)
        {
            if (GstCalculator.Round(item.PurchasePrice) != item.PurchasePrice)
                throw new BusinessException("Purchase prices can have at most 2 decimal places.");
        }

        var supplierInvoice = string.IsNullOrWhiteSpace(request.SupplierInvoiceNumber)
            ? null
            : request.SupplierInvoiceNumber.Trim();

        await using var transaction = await _context.Database.BeginTransactionAsync();

        var supplier = await _context.Suppliers
            .FirstOrDefaultAsync(x => x.Id == request.SupplierId && !x.IsDeleted)
            ?? throw new BusinessException("Supplier not found or has been deleted.");

        // The same supplier bill entered twice is almost always a mistake.
        if (supplierInvoice != null &&
            await _context.Purchases.AnyAsync(p =>
                p.SupplierId == supplier.Id &&
                p.SupplierInvoiceNumber == supplierInvoice))
        {
            throw BusinessException.Conflict(
                $"A purchase with supplier invoice number '{supplierInvoice}' already exists for this supplier.");
        }

        var company = await _context.Companies
            .AsNoTracking()
            .OrderBy(x => x.Id)
            .FirstOrDefaultAsync()
            ?? throw BusinessException.Conflict(
                "Company settings are not configured. Open Settings and save your company details first.");

        var productIds = request.Items.Select(i => i.ProductId).Distinct().ToList();

        var products = await _context.Products
            .Where(x => productIds.Contains(x.Id) && !x.IsDeleted)
            .ToDictionaryAsync(x => x.Id);

        var missing = productIds.Where(id => !products.ContainsKey(id)).ToList();

        if (missing.Count > 0)
        {
            throw new BusinessException(
                $"Product not found or has been deleted (id: {string.Join(", ", missing)}).");
        }

        var taxIds = products.Values
            .Where(p => p.TaxId.HasValue)
            .Select(p => p.TaxId!.Value)
            .Distinct()
            .ToList();

        var taxes = await _context.Taxes
            .AsNoTracking()
            .Where(t => taxIds.Contains(t.Id))
            .ToDictionaryAsync(t => t.Id);

        var problems = new List<string>();

        foreach (var product in products.Values)
        {
            if (product.TaxId is { } taxId &&
                (!taxes.TryGetValue(taxId, out var tax) || tax.IsDeleted || !tax.IsActive))
            {
                problems.Add(
                    $"The tax assigned to '{product.Name}' is inactive or removed. Fix the product's tax first.");
            }
        }

        if (problems.Count > 0)
            throw BusinessException.Conflict(string.Join(" ", problems));

        // ---- Input tax: supplier state vs company state ----

        var companyState = PlaceOfSupply.CompanyStateCode(
            company.StateCode, company.State, company.GSTNumber);

        var supplierState = PlaceOfSupply.PartyStateCode(
            supplier.StateCode, supplier.State, supplier.Gstin);

        var isInterState = PlaceOfSupply.IsInterState(
            PlaceOfSupply.Resolve(supplierState, companyState), companyState);

        var hasTaxableLine = products.Values.Any(p =>
            p.TaxId.HasValue && taxes[p.TaxId.Value].Percentage > 0);

        if (hasTaxableLine && companyState == null)
        {
            throw BusinessException.Conflict(
                "Set your company's state in Settings before recording taxable purchases (needed to choose CGST/SGST or IGST).");
        }

        // ---- Build the purchase ----

        var purchase = new Purchase
        {
            SupplierId = supplier.Id,
            SupplierInvoiceNumber = supplierInvoice,
            PurchaseDate = purchaseDate,
            Remarks = string.IsNullOrWhiteSpace(request.Remarks) ? null : request.Remarks.Trim(),
            CreatedOn = DateTime.UtcNow,
            CreatedBy = _user.Email,
            ClientRequestId = request.ClientRequestId,
            TaxCalculated = true,
            IsInterState = isInterState,
            SupplierGstin = GstStates.NormalizeGstin(supplier.Gstin)
        };

        var taxLines = new List<(decimal Taxable, TaxBreakup Tax)>();

        foreach (var line in request.Items)
        {
            var product = products[line.ProductId];
            var tax = product.TaxId.HasValue ? taxes[product.TaxId.Value] : null;
            var rate = tax?.Percentage ?? 0m;

            var taxable = GstCalculator.LineTaxable(line.Quantity, line.PurchasePrice);
            var breakup = GstCalculator.CalculateTax(taxable, rate, isInterState);

            taxLines.Add((taxable, breakup));

            purchase.PurchaseItems.Add(new PurchaseItem
            {
                ProductId = product.Id,
                Quantity = line.Quantity,
                PurchasePrice = line.PurchasePrice,
                Total = taxable,
                HsnCode = product.HsnCode,
                TaxName = tax?.Name,
                TaxRate = rate,
                TaxableAmount = taxable,
                CgstAmount = breakup.Cgst,
                SgstAmount = breakup.Sgst,
                IgstAmount = breakup.Igst,
                TaxAmount = breakup.Total
            });

            if (request.UpdateCatalogueCost)
                product.PurchasePrice = line.PurchasePrice;
        }

        var totals = GstCalculator.Total(taxLines);

        purchase.Subtotal = totals.Subtotal;
        purchase.CgstTotal = totals.Cgst;
        purchase.SgstTotal = totals.Sgst;
        purchase.IgstTotal = totals.Igst;
        purchase.TaxTotal = totals.Tax;
        purchase.RoundOff = totals.RoundOff;
        purchase.TotalAmount = totals.GrandTotal;

        try
        {
            var sequence = await _numbers.NextAsync(
                DocumentNumbering.SeriesKey(DocumentNumbering.PurchasePrefix, purchaseDate));

            purchase.PurchaseNumber = DocumentNumbering.Format(
                DocumentNumbering.PurchasePrefix, purchaseDate, sequence);
        }
        catch (ArgumentOutOfRangeException)
        {
            throw BusinessException.Conflict(
                "The purchase number series for this financial year is exhausted.");
        }

        _context.Purchases.Add(purchase);

        // Stock in (one ledger row per line).
        foreach (var line in request.Items
                     .Select((item, index) => (item, index))
                     .OrderBy(x => x.item.ProductId)
                     .ThenBy(x => x.index))
        {
            await _inventoryService.AdjustStockAsync(
                line.item.ProductId,
                line.item.Quantity,
                StockTransactionType.Purchase,
                purchase.PurchaseNumber,
                "Purchase Entry");
        }

        await _context.SaveChangesAsync();

        _audit.Record(
            "Purchase.Create", "Purchase", purchase.Id.ToString(),
            $"{purchase.PurchaseNumber}; supplier {supplier.Id}; total {purchase.TotalAmount:0.00}");

        await _context.SaveChangesAsync();
        await transaction.CommitAsync();

        return new PurchaseResponse
        {
            PurchaseId = purchase.Id,
            PurchaseNumber = purchase.PurchaseNumber,
            TotalAmount = purchase.TotalAmount
        };
    }

    private async Task<PurchaseResponse?> FindByRequestIdAsync(Guid requestId)
    {
        return await _context.Purchases
            .AsNoTracking()
            .Where(x => x.ClientRequestId == requestId)
            .Select(x => new PurchaseResponse
            {
                PurchaseId = x.Id,
                PurchaseNumber = x.PurchaseNumber,
                TotalAmount = x.TotalAmount,
                IsDuplicate = true
            })
            .FirstOrDefaultAsync();
    }

    private static bool IsUniqueViolation(DbUpdateException ex) =>
        ex.InnerException is SqlException { Number: 2601 or 2627 };

    // ------------------------------------------------------------------
    // List
    // ------------------------------------------------------------------

    public async Task<PagedResult<PurchaseListItemDto>> SearchAsync(PurchaseSearchRequest request)
    {
        var pageNumber = Math.Max(request.PageNumber, 1);
        var pageSize = Math.Clamp(request.PageSize, 1, MaxPageSize);

        var query = _context.Purchases.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var search = request.Search.Trim();

            query = query.Where(x =>
                x.PurchaseNumber.Contains(search) ||
                (x.SupplierInvoiceNumber != null && x.SupplierInvoiceNumber.Contains(search)) ||
                x.Supplier.Name.Contains(search) ||
                x.Supplier.CompanyName.Contains(search));
        }

        if (request.SupplierId.HasValue)
            query = query.Where(x => x.SupplierId == request.SupplierId.Value);

        if (request.FromDate.HasValue)
        {
            var from = request.FromDate.Value.Date;
            query = query.Where(x => x.PurchaseDate >= from);
        }

        if (request.ToDate.HasValue)
        {
            var toExclusive = request.ToDate.Value.Date.AddDays(1);
            query = query.Where(x => x.PurchaseDate < toExclusive);
        }

        int totalRecords = await query.CountAsync();

        // Suppliers are joined without a soft-delete filter on purpose:
        // history must keep showing deleted suppliers.
        var data = await query
            .OrderByDescending(x => x.PurchaseDate)
            .ThenByDescending(x => x.Id)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new PurchaseListItemDto
            {
                Id = x.Id,
                PurchaseNumber = x.PurchaseNumber,
                SupplierInvoiceNumber = x.SupplierInvoiceNumber,
                SupplierId = x.SupplierId,
                SupplierName = x.Supplier.CompanyName != ""
                    ? x.Supplier.CompanyName
                    : x.Supplier.Name,
                PurchaseDate = x.PurchaseDate,
                TotalAmount = x.TotalAmount,
                ItemCount = x.PurchaseItems.Count,
                ReturnedAmount = _context.PurchaseReturns
                    .Where(r => r.PurchaseId == x.Id)
                    .Sum(r => (decimal?)r.TotalAmount) ?? 0
            })
            .ToListAsync();

        return new PagedResult<PurchaseListItemDto>
        {
            TotalRecords = totalRecords,
            TotalPages = (int)Math.Ceiling((double)totalRecords / pageSize),
            PageNumber = pageNumber,
            PageSize = pageSize,
            Data = data
        };
    }

    // ------------------------------------------------------------------
    // Details
    // ------------------------------------------------------------------

    public async Task<PurchaseDetailDto> GetByIdAsync(int id)
    {
        var purchase = await _context.Purchases
            .AsNoTracking()
            .Include(p => p.Supplier)
            .Include(p => p.PurchaseItems).ThenInclude(i => i.Product)
            .FirstOrDefaultAsync(p => p.Id == id)
            ?? throw BusinessException.NotFound("Purchase not found.");

        var returns = await _context.PurchaseReturns
            .AsNoTracking()
            .Where(r => r.PurchaseId == id)
            .OrderBy(r => r.ReturnDate).ThenBy(r => r.Id)
            .Select(r => new PurchaseReturnSummaryDto
            {
                Id = r.Id,
                ReturnNumber = r.ReturnNumber,
                ReturnDate = r.ReturnDate,
                TotalAmount = r.TotalAmount,
                RefundReceived = r.RefundReceived
            })
            .ToListAsync();

        var returnedQuantities = await _context.PurchaseReturnItems
            .AsNoTracking()
            .Where(r => r.PurchaseReturn.PurchaseId == id)
            .GroupBy(r => r.PurchaseItemId)
            .Select(g => new { ItemId = g.Key, Quantity = g.Sum(x => x.Quantity) })
            .ToDictionaryAsync(x => x.ItemId, x => x.Quantity);

        var supplier = purchase.Supplier;

        return new PurchaseDetailDto
        {
            Id = purchase.Id,
            PurchaseNumber = purchase.PurchaseNumber,
            SupplierInvoiceNumber = purchase.SupplierInvoiceNumber,
            PurchaseDate = purchase.PurchaseDate,
            Remarks = purchase.Remarks,
            CreatedBy = purchase.CreatedBy,
            Supplier = new PurchaseSupplierDto
            {
                Id = supplier.Id,
                Name = supplier.Name,
                CompanyName = supplier.CompanyName,
                Mobile = supplier.Mobile,
                Email = supplier.Email,
                Address = supplier.Address,
                State = supplier.State,
                StateCode = supplier.StateCode,
                // The GSTIN on the purchase is what applied at the time.
                Gstin = purchase.SupplierGstin ?? supplier.Gstin
            },
            Subtotal = purchase.Subtotal,
            CgstTotal = purchase.CgstTotal,
            SgstTotal = purchase.SgstTotal,
            IgstTotal = purchase.IgstTotal,
            TaxTotal = purchase.TaxTotal,
            RoundOff = purchase.RoundOff,
            TotalAmount = purchase.TotalAmount,
            TaxCalculated = purchase.TaxCalculated,
            IsInterState = purchase.IsInterState,
            ReturnedAmount = returns.Sum(r => r.TotalAmount),
            Returns = returns,
            Items = purchase.PurchaseItems
                .OrderBy(i => i.Id)
                .Select(i =>
                {
                    returnedQuantities.TryGetValue(i.Id, out var returned);

                    return new PurchaseDetailItemDto
                    {
                        Id = i.Id,
                        ProductId = i.ProductId,
                        ProductName = i.Product.Name,
                        Sku = i.Product.SKU,
                        HsnCode = i.HsnCode,
                        Quantity = i.Quantity,
                        UnitPrice = i.PurchasePrice,
                        Total = i.TaxableAmount ?? i.Total,
                        TaxName = i.TaxName,
                        TaxRate = i.TaxRate,
                        CgstAmount = i.CgstAmount,
                        SgstAmount = i.SgstAmount,
                        IgstAmount = i.IgstAmount,
                        TaxAmount = i.TaxAmount,
                        LineTotal = i.TaxableAmount.HasValue
                            ? i.TaxableAmount.Value + (i.TaxAmount ?? 0)
                            : i.Total,
                        ReturnedQuantity = returned,
                        ReturnableQuantity = Math.Max(0, i.Quantity - returned)
                    };
                })
                .ToList()
        };
    }
}