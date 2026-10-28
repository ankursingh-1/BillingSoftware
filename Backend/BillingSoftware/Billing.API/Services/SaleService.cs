using Billing.API.Models;
using Billing.Application.DTOs.Sales;
using Billing.Application.Interfaces;
using Billing.Domain.Entities;
using Billing.Domain.Enums;
using Billing.Persistence.Context;
using Billing.Shared.Exceptions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace Billing.API.Services;

public class SaleService
{
    private const int MaxPageSize = 100;

    // Name of the SQL Server application lock that serialises sale creation.
    // See AcquireSaleLockAsync for why it exists and what it does not cover.
    private const string SaleCreateLockResource = "Billing.Sales.Create";
    private const int SaleCreateLockTimeoutMs = 15000;

    private readonly BillingDbContext _context;
    private readonly IInventoryService _inventoryService;

    public SaleService(
        BillingDbContext context,
        IInventoryService inventoryService)
    {
        _context = context;
        _inventoryService = inventoryService;
    }

    public async Task<SaleResponse> CreateAsync(CreateSaleRequest dto)
    {
        // Merge repeated product lines so each product is validated, priced
        // and deducted from stock exactly once.
        var lines = dto.Items
            .GroupBy(i => i.ProductId)
            .Select(g => new SaleLine(
                g.Key,
                g.Sum(i => i.Quantity),
                g.Where(i => i.UnitPrice.HasValue)
                 .Select(i => i.UnitPrice!.Value)
                 .Distinct()
                 .ToList()))
            .ToList();

        // Disposing an uncommitted transaction rolls it back, so any
        // rejection below leaves no sale, no stock change and no ledger entry.
        await using var transaction =
            await _context.Database.BeginTransactionAsync();

        await AcquireSaleLockAsync(transaction);

        // ---- Validate everything before writing anything ----

        if (!await _context.Customers.AnyAsync(x =>
                x.Id == dto.CustomerId && !x.IsDeleted))
        {
            throw new BusinessException(
                "Customer not found or has been deleted.");
        }

        var productIds = lines.Select(l => l.ProductId).ToList();

        var products = await _context.Products
            .Where(x => productIds.Contains(x.Id) && !x.IsDeleted)
            .ToDictionaryAsync(x => x.Id);

        var missing = productIds.Where(id => !products.ContainsKey(id)).ToList();

        if (missing.Count > 0)
        {
            throw new BusinessException(
                $"Product not found or has been deleted (id: {string.Join(", ", missing)}).");
        }

        var conflicts = new List<string>();

        foreach (var line in lines)
        {
            var product = products[line.ProductId];

            // A client-supplied price is only an echo of what the user saw.
            // It never sets the price; a mismatch means the cart is stale.
            if (line.EchoedPrices.Any(p => p != product.SellingPrice))
            {
                conflicts.Add(
                    $"The price of '{product.Name}' has changed to {product.SellingPrice:0.00}. Refresh and try again.");
            }

            if (product.Stock <= 0)
            {
                conflicts.Add($"'{product.Name}' is out of stock.");
            }
            else if (product.Stock < line.Quantity)
            {
                conflicts.Add(
                    $"'{product.Name}': only {product.Stock} in stock, {line.Quantity} requested.");
            }
        }

        if (conflicts.Count > 0)
        {
            throw BusinessException.Conflict(string.Join(" ", conflicts));
        }

        // ---- Build the sale; every amount comes from the catalogue ----

        var sale = new Sale
        {
            InvoiceNumber = await GenerateInvoiceNumber(),
            CustomerId = dto.CustomerId,
            // SaleDate is the business date used by the dashboard and reports
            // (they bucket by the server's local calendar day), so it stays
            // local. CreatedOn is the audit timestamp and is UTC.
            SaleDate = DateTime.Now,
            CreatedOn = DateTime.UtcNow,
            Remarks = null,
            TotalAmount = 0
        };

        decimal grandTotal = 0;

        foreach (var line in lines)
        {
            var product = products[line.ProductId];

            decimal unitPrice = product.SellingPrice;
            decimal lineTotal = Math.Round(
                line.Quantity * unitPrice,
                2,
                MidpointRounding.AwayFromZero);

            sale.SaleItems.Add(new SaleItem
            {
                ProductId = product.Id,
                Quantity = line.Quantity,
                SellingPrice = unitPrice,
                Total = lineTotal
            });

            grandTotal += lineTotal;
        }

        sale.TotalAmount = grandTotal;

        _context.Sales.Add(sale);

        // One stock deduction (and one ledger row) per product.
        foreach (var line in lines)
        {
            await _inventoryService.AdjustStockAsync(
                line.ProductId,
                line.Quantity,
                StockTransactionType.Sale,
                sale.InvoiceNumber,
                "Sales Entry");
        }

        await _context.SaveChangesAsync();
        await transaction.CommitAsync();

        return new SaleResponse
        {
            SaleId = sale.Id,
            InvoiceNumber = sale.InvoiceNumber,
            TotalAmount = sale.TotalAmount
        };
    }

    public async Task<PagedResult<SaleListItemDto>> SearchAsync(
        SaleSearchRequest request)
    {
        var pageNumber = Math.Max(request.PageNumber, 1);
        var pageSize = Math.Clamp(request.PageSize, 1, MaxPageSize);

        var query = _context.Sales.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var search = request.Search.Trim();

            query = query.Where(x =>
                x.InvoiceNumber.Contains(search) ||
                x.Customer.Name.Contains(search));
        }

        if (request.CustomerId.HasValue)
        {
            query = query.Where(x => x.CustomerId == request.CustomerId.Value);
        }

        if (request.FromDate.HasValue)
        {
            var from = request.FromDate.Value.Date;
            query = query.Where(x => x.SaleDate >= from);
        }

        if (request.ToDate.HasValue)
        {
            var toExclusive = request.ToDate.Value.Date.AddDays(1);
            query = query.Where(x => x.SaleDate < toExclusive);
        }

        int totalRecords = await query.CountAsync();

        // Customers are joined without a soft-delete filter on purpose:
        // history must keep showing deleted customers.
        var data = await query
            .OrderByDescending(x => x.SaleDate)
            .ThenByDescending(x => x.Id)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new SaleListItemDto
            {
                Id = x.Id,
                InvoiceNumber = x.InvoiceNumber,
                CustomerId = x.CustomerId,
                CustomerName = x.Customer.Name,
                SaleDate = x.SaleDate,
                TotalAmount = x.TotalAmount,
                ItemCount = x.SaleItems.Count
            })
            .ToListAsync();

        return new PagedResult<SaleListItemDto>
        {
            TotalRecords = totalRecords,
            TotalPages = (int)Math.Ceiling((double)totalRecords / pageSize),
            PageNumber = pageNumber,
            PageSize = pageSize,
            Data = data
        };
    }

    public async Task<SaleDetailDto> GetByIdAsync(int id)
    {
        var sale = await _context.Sales
            .AsNoTracking()
            .Where(x => x.Id == id)
            .Select(x => new SaleDetailDto
            {
                Id = x.Id,
                InvoiceNumber = x.InvoiceNumber,
                SaleDate = x.SaleDate,
                TotalAmount = x.TotalAmount,
                Remarks = x.Remarks,
                Customer = new SaleCustomerDto
                {
                    Id = x.Customer.Id,
                    Name = x.Customer.Name,
                    Mobile = x.Customer.Mobile,
                    Email = x.Customer.Email,
                    Address = x.Customer.Address
                },
                Items = x.SaleItems
                    .OrderBy(i => i.Id)
                    .Select(i => new SaleDetailItemDto
                    {
                        ProductId = i.ProductId,
                        ProductName = i.Product.Name,
                        Sku = i.Product.SKU,
                        Quantity = i.Quantity,
                        UnitPrice = i.SellingPrice,
                        Total = i.Total
                    })
                    .ToList()
            })
            .FirstOrDefaultAsync();

        return sale ?? throw BusinessException.NotFound("Sale not found.");
    }

    /// <summary>
    /// Serialises sale creation using a transaction-scoped SQL Server
    /// application lock (released automatically on commit/rollback).
    ///
    /// Why: stock and the invoice number are both "read, decide, write".
    /// Without a lock, two simultaneous sales can both pass the stock check
    /// for the last unit, or both read the same sales count and produce the
    /// same invoice number. The lock makes concurrent CreateAsync calls run
    /// one after another.
    ///
    /// What it does NOT cover (needs a schema change to fix properly):
    /// stock changes made by purchases, returns and stock adjustments do not
    /// take this lock, and Product has no concurrency token (rowversion), so a
    /// sale racing one of those can still lose an update. Invoice numbers are
    /// still count-based and have no unique index; the planned yearly sequence
    /// table plus unique index replaces this lock for numbering.
    /// </summary>
    private async Task AcquireSaleLockAsync(IDbContextTransaction transaction)
    {
        var connection = _context.Database.GetDbConnection();

        await using var command = connection.CreateCommand();
        command.Transaction = transaction.GetDbTransaction();
        command.CommandText =
            "DECLARE @result int; " +
            $"EXEC @result = sp_getapplock @Resource = N'{SaleCreateLockResource}', " +
            "@LockMode = 'Exclusive', @LockOwner = 'Transaction', " +
            $"@LockTimeout = {SaleCreateLockTimeoutMs}; " +
            "SELECT @result;";

        var result = Convert.ToInt32(await command.ExecuteScalarAsync());

        // 0 = granted, 1 = granted after waiting; negative = not granted.
        if (result < 0)
        {
            throw BusinessException.Conflict(
                "Another sale is being processed. Please try again.");
        }
    }

    private async Task<string> GenerateInvoiceNumber()
    {
        int count = await _context.Sales.CountAsync() + 1;
        return $"INV-{DateTime.Now:yyyyMMdd}-{count:D6}";
    }

    private sealed record SaleLine(
        int ProductId,
        int Quantity,
        List<decimal> EchoedPrices);
}
