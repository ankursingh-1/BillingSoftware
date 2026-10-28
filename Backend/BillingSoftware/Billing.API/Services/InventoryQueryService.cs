using Billing.API.Models;
using Billing.Application.DTOs.Inventory;
using Billing.Application.Inventory;
using Billing.Persistence.Context;
using Billing.Shared.Exceptions;
using Microsoft.EntityFrameworkCore;

namespace Billing.API.Services;

/// <summary>Read-only current-stock views for the inventory pages.</summary>
public class InventoryQueryService
{
    private const int MaxPageSize = 200;

    private readonly BillingDbContext _context;
    private readonly StockAlertPolicy _policy;

    public InventoryQueryService(BillingDbContext context, StockAlertPolicy policy)
    {
        _context = context;
        _policy = policy;
    }

    public async Task<PagedResult<StockItemDto>> SearchAsync(StockSearchRequest request)
    {
        var pageNumber = Math.Max(request.PageNumber, 1);
        var pageSize = Math.Clamp(request.PageSize, 1, MaxPageSize);
        var threshold = _policy.LowStockThreshold;

        var query = _context.Products.AsNoTracking().Where(x => !x.IsDeleted);

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var search = request.Search.Trim();
            query = query.Where(x => x.Name.Contains(search) || x.SKU.Contains(search));
        }

        if (request.CategoryId.HasValue)
            query = query.Where(x => x.CategoryId == request.CategoryId.Value);

        switch (request.Status?.Trim().ToLowerInvariant())
        {
            case null or "" or "all":
                break;
            case "in":
                query = query.Where(x => x.Stock > threshold);
                break;
            case "low":
                query = query.Where(x => x.Stock > 0 && x.Stock <= threshold);
                break;
            case "out":
                query = query.Where(x => x.Stock <= 0);
                break;
            default:
                throw new BusinessException("Unknown stock status filter.");
        }

        int totalRecords = await query.CountAsync();

        // Lowest stock first so problems are at the top.
        var rows = await query
            .OrderBy(x => x.Stock)
            .ThenBy(x => x.Name)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new
            {
                x.Id,
                x.Name,
                x.SKU,
                CategoryName = x.Category != null ? x.Category.Name : null,
                UnitName = x.Unit != null ? x.Unit.Name : null,
                x.Stock,
                x.PurchasePrice,
                x.SellingPrice
            })
            .ToListAsync();

        return new PagedResult<StockItemDto>
        {
            TotalRecords = totalRecords,
            TotalPages = (int)Math.Ceiling((double)totalRecords / pageSize),
            PageNumber = pageNumber,
            PageSize = pageSize,
            Data = rows.Select(x => new StockItemDto
            {
                ProductId = x.Id,
                Name = x.Name,
                Sku = x.SKU,
                CategoryName = x.CategoryName,
                UnitName = x.UnitName,
                Stock = x.Stock,
                Status = _policy.Classify(x.Stock).ToString(),
                PurchasePrice = x.PurchasePrice,
                SellingPrice = x.SellingPrice,
                StockValue = x.PurchasePrice > 0 ? x.Stock * x.PurchasePrice : null
            }).ToList()
        };
    }

    public async Task<StockSummaryDto> GetSummaryAsync()
    {
        var threshold = _policy.LowStockThreshold;

        var products = _context.Products.AsNoTracking().Where(x => !x.IsDeleted);

        var totals = await products
            .GroupBy(_ => 1)
            .Select(g => new
            {
                Total = g.Count(),
                Out = g.Count(x => x.Stock <= 0),
                Low = g.Count(x => x.Stock > 0 && x.Stock <= threshold),
                Units = g.Sum(x => x.Stock > 0 ? x.Stock : 0),
                Value = g.Sum(x => x.PurchasePrice > 0 && x.Stock > 0 ? x.Stock * x.PurchasePrice : 0m),
                NoCost = g.Count(x => x.PurchasePrice <= 0)
            })
            .FirstOrDefaultAsync();

        if (totals == null)
            return new StockSummaryDto { LowStockThreshold = threshold };

        return new StockSummaryDto
        {
            TotalProducts = totals.Total,
            OutOfStock = totals.Out,
            LowStock = totals.Low,
            InStock = totals.Total - totals.Out - totals.Low,
            LowStockThreshold = threshold,
            TotalUnits = totals.Units,
            StockValueAtCost = totals.Value,
            ProductsWithoutCost = totals.NoCost
        };
    }
}