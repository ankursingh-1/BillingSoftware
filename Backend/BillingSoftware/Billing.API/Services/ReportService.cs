using Billing.Application.DTOs.Reports;
using Billing.Persistence.Context;
using Microsoft.EntityFrameworkCore;

namespace Billing.API.Services;

public class ReportService
{
    private readonly BillingDbContext _context;

    public ReportService(BillingDbContext context)
    {
        _context = context;
    }

    public async Task<SalesReportResponse> GetSalesReportAsync(SalesReportRequest request)
    {
        var query = _context.Sales
            .AsNoTracking()
            .Include(x => x.Customer)
            .AsQueryable();

        if (request.FromDate.HasValue)
        {
            query = query.Where(x => x.SaleDate >= request.FromDate.Value);
        }

        if (request.ToDate.HasValue)
        {
            query = query.Where(x => x.SaleDate <= request.ToDate.Value);
        }

        if (request.CustomerId.HasValue)
        {
            query = query.Where(x => x.CustomerId == request.CustomerId.Value);
        }

        if (!string.IsNullOrWhiteSpace(request.InvoiceNumber))
        {
            query = query.Where(x =>
                x.InvoiceNumber.Contains(request.InvoiceNumber));
        }

        var totalRecords = await query.CountAsync();

        var grandTotal = await query.SumAsync(x => x.TotalAmount);

        var items = await query
            .OrderByDescending(x => x.SaleDate)
            .Skip((request.PageNumber - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(x => new SalesReportItemDto
            {
                SaleId = x.Id,
                InvoiceNumber = x.InvoiceNumber,
                CustomerName = x.Customer.Name,
                SaleDate = x.SaleDate,
                TotalAmount = x.TotalAmount
            })
            .ToListAsync();

        return new SalesReportResponse
        {
            PageNumber = request.PageNumber,
            PageSize = request.PageSize,
            TotalRecords = totalRecords,
            TotalPages = (int)Math.Ceiling((double)totalRecords / request.PageSize),
            GrandTotal = grandTotal,
            Items = items
        };
    }
    public async Task<PurchaseReportResponse> GetPurchaseReportAsync(PurchaseReportRequest request)
    {
        var query = _context.Purchases
            .AsNoTracking()
            .Include(x => x.Supplier)
            .AsQueryable();

        if (request.FromDate.HasValue)
            query = query.Where(x => x.PurchaseDate >= request.FromDate.Value);

        if (request.ToDate.HasValue)
            query = query.Where(x => x.PurchaseDate <= request.ToDate.Value);

        if (request.SupplierId.HasValue)
            query = query.Where(x => x.SupplierId == request.SupplierId.Value);

        if (!string.IsNullOrWhiteSpace(request.PurchaseNumber))
            query = query.Where(x => x.PurchaseNumber.Contains(request.PurchaseNumber));

        var totalRecords = await query.CountAsync();

        var grandTotal = await query.SumAsync(x => x.TotalAmount);

        var items = await query
            .OrderByDescending(x => x.PurchaseDate)
            .Skip((request.PageNumber - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(x => new PurchaseReportItemDto
            {
                PurchaseId = x.Id,
                PurchaseNumber = x.PurchaseNumber,
                SupplierName = x.Supplier.Name,
                PurchaseDate = x.PurchaseDate,
                TotalAmount = x.TotalAmount
            })
            .ToListAsync();

        return new PurchaseReportResponse
        {
            PageNumber = request.PageNumber,
            PageSize = request.PageSize,
            TotalRecords = totalRecords,
            TotalPages = (int)Math.Ceiling((double)totalRecords / request.PageSize),
            GrandTotal = grandTotal,
            Items = items
        };
    }

    public async Task<StockReportResponse> GetStockReportAsync(StockReportRequest request)
    {
        var query = _context.Products
            .AsNoTracking()
            .Where(x => !x.IsDeleted)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            query = query.Where(x =>
                x.Name.Contains(request.Search) ||
                x.SKU.Contains(request.Search));
        }

        if (request.LowStockOnly == true)
        {
            query = query.Where(x => x.Stock > 0 && x.Stock <= 10);
        }

        if (request.OutOfStockOnly == true)
        {
            query = query.Where(x => x.Stock == 0);
        }

        var totalRecords = await query.CountAsync();

        var totalStockValue = await query.SumAsync(x => x.Stock * x.PurchasePrice);

        var items = await query
            .OrderBy(x => x.Name)
            .Skip((request.PageNumber - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(x => new StockReportItemDto
            {
                ProductId = x.Id,
                ProductName = x.Name,
                SKU = x.SKU,
                CurrentStock = x.Stock,
                PurchasePrice = x.PurchasePrice,
                SellingPrice = x.SellingPrice,
                StockValue = x.Stock * x.PurchasePrice
            })
            .ToListAsync();

        return new StockReportResponse
        {
            PageNumber = request.PageNumber,
            PageSize = request.PageSize,
            TotalRecords = totalRecords,
            TotalPages = (int)Math.Ceiling((double)totalRecords / request.PageSize),
            TotalStockValue = totalStockValue,
            Items = items
        };
    }
}