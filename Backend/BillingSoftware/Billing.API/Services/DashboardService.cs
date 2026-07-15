using Billing.Application.DTOs.Dashboard;
using Billing.Persistence.Context;
using Microsoft.EntityFrameworkCore;

namespace Billing.API.Services;

public class DashboardService
{
    private readonly BillingDbContext _context;

    public DashboardService(BillingDbContext context)
    {
        _context = context;
    }
    public async Task<List<RecentSaleDto>> GetRecentSalesAsync(int count = 10)
    {
        return await _context.Sales
            .AsNoTracking()
            .Include(x => x.Customer)
            .OrderByDescending(x => x.SaleDate)
            .Take(count)
            .Select(x => new RecentSaleDto
            {
                SaleId = x.Id,
                InvoiceNumber = x.InvoiceNumber,
                CustomerName = x.Customer.Name,
                TotalAmount = x.TotalAmount,
                SaleDate = x.SaleDate
            })
            .ToListAsync();
    }

    public async Task<List<RecentPurchaseDto>> GetRecentPurchasesAsync(int count = 10)
    {
        return await _context.Purchases
            .AsNoTracking()
            .Include(x => x.Supplier)
            .OrderByDescending(x => x.PurchaseDate)
            .Take(count)
            .Select(x => new RecentPurchaseDto
            {
                PurchaseId = x.Id,
                PurchaseNumber = x.PurchaseNumber,
                SupplierName = x.Supplier.Name,
                TotalAmount = x.TotalAmount,
                PurchaseDate = x.PurchaseDate
            })
            .ToListAsync();
    }

    public async Task<List<LowStockDto>> GetLowStockProductsAsync(int threshold = 5)
    {
        return await _context.Products
            .AsNoTracking()
            .Where(x => !x.IsDeleted &&
                        x.Stock <= threshold)
            .OrderBy(x => x.Stock)
            .Select(x => new LowStockDto
            {
                ProductId = x.Id,
                ProductName = x.Name,
                CurrentStock = x.Stock
            })
            .ToListAsync();
    }

    public async Task<DashboardSummaryDto> GetSummaryAsync()
    {
        var today = DateTime.Today;
        var monthStart = new DateTime(today.Year, today.Month, 1);

        var totalSales = await _context.Sales
            .AsNoTracking()
            .SumAsync(x => (decimal?)x.TotalAmount) ?? 0;

        var todaySales = await _context.Sales
            .AsNoTracking()
            .Where(x => x.SaleDate.Date == today)
            .SumAsync(x => (decimal?)x.TotalAmount) ?? 0;

        var monthlySales = await _context.Sales
            .AsNoTracking()
            .Where(x => x.SaleDate >= monthStart)
            .SumAsync(x => (decimal?)x.TotalAmount) ?? 0;

        var totalPurchase = await _context.Purchases
            .AsNoTracking()
            .SumAsync(x => (decimal?)x.TotalAmount) ?? 0;

        var todayPurchase = await _context.Purchases
            .AsNoTracking()
            .Where(x => x.PurchaseDate.Date == today)
            .SumAsync(x => (decimal?)x.TotalAmount) ?? 0;

        var monthlyPurchase = await _context.Purchases
            .AsNoTracking()
            .Where(x => x.PurchaseDate >= monthStart)
            .SumAsync(x => (decimal?)x.TotalAmount) ?? 0;

        var totalProducts = await _context.Products
            .AsNoTracking()
            .CountAsync(x => !x.IsDeleted);

        var totalCustomers = await _context.Customers
            .AsNoTracking()
            .CountAsync();

        var totalSuppliers = await _context.Suppliers
            .AsNoTracking()
            .CountAsync();

        var lowStockProducts = await _context.Products
            .AsNoTracking()
            .CountAsync(x => !x.IsDeleted && x.Stock > 0 && x.Stock <= 5);

        var outOfStockProducts = await _context.Products
            .AsNoTracking()
            .CountAsync(x => !x.IsDeleted && x.Stock == 0);

        return new DashboardSummaryDto
        {
            TotalSales = totalSales,
            TodaySales = todaySales,
            MonthlySales = monthlySales,

            TotalPurchase = totalPurchase,
            TodayPurchase = todayPurchase,
            MonthlyPurchase = monthlyPurchase,

            GrossProfit = totalSales - totalPurchase,

            TotalProducts = totalProducts,
            TotalCustomers = totalCustomers,
            TotalSuppliers = totalSuppliers,

            LowStockProducts = lowStockProducts,
            OutOfStockProducts = outOfStockProducts
        };
    }
    public async Task<List<TopProductDto>> GetTopSellingProductsAsync(int count = 10)
    {
        return await _context.SaleItems
            .AsNoTracking()
            .Include(x => x.Product)
            .GroupBy(x => new
            {
                x.ProductId,
                x.Product.Name
            })
            .Select(g => new TopProductDto
            {
                ProductId = g.Key.ProductId,
                ProductName = g.Key.Name,
                TotalSold = g.Sum(x => x.Quantity),
                SalesAmount = g.Sum(x => x.Total)
            })
            .OrderByDescending(x => x.TotalSold)
            .Take(count)
            .ToListAsync();
    }
}