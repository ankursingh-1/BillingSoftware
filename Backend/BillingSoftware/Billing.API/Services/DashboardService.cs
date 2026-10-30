using Billing.Application.Accounting;
using Billing.Application.DTOs.Dashboard;
using Billing.Application.Inventory;
using Billing.Application.Warranty;
using Billing.Persistence;
using Billing.Persistence.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Billing.API.Services;

public class DashboardService
{
    private readonly BillingDbContext _context;
    private readonly StockAlertPolicy _alerts;
    private readonly GrossProfitService _grossProfit;
    private readonly ILogger<DashboardService> _logger;

    public DashboardService(
        BillingDbContext context,
        StockAlertPolicy alerts,
        GrossProfitService grossProfit,
        ILogger<DashboardService> logger)
    {
        _context = context;
        _alerts = alerts;
        _grossProfit = grossProfit;
        _logger = logger;
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

    public async Task<List<LowStockDto>> GetLowStockProductsAsync(int? threshold = null)
    {
        var limit = threshold ?? _alerts.LowStockThreshold;

        return await _context.Products
            .AsNoTracking()
            .Where(x => !x.IsDeleted &&
                        x.Stock > 0 &&
                        x.Stock <= limit)
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

        var todayEnd = today.AddDays(1);

        var todaySales = await _context.Sales
            .AsNoTracking()
            .Where(x => x.SaleDate >= today && x.SaleDate < todayEnd)
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
            .Where(x => x.PurchaseDate >= today && x.PurchaseDate < todayEnd)
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
            .CountAsync(x => !x.IsDeleted);

        var totalSuppliers = await _context.Suppliers
            .AsNoTracking()
            .CountAsync(x => !x.IsDeleted);

        var lowStockProducts = await _context.Products
            .AsNoTracking()
            .CountAsync(x => !x.IsDeleted && x.Stock > 0 && x.Stock <= _alerts.LowStockThreshold);

        var outOfStockProducts = await _context.Products
            .AsNoTracking()
            .CountAsync(x => !x.IsDeleted && x.Stock == 0);

        var monthProfit = await _grossProfit.ForPeriodAsync(monthStart, monthStart.AddMonths(1));

        var (expiring, expiringAvailable) = await CountExpiringWarrantiesSafeAsync(today);
        var outstanding = await SumOutstandingCollectionsAsync();

        return new DashboardSummaryDto
        {
            TotalSales = totalSales,
            TodaySales = todaySales,
            MonthlySales = monthlySales,

            TotalPurchase = totalPurchase,
            TodayPurchase = todayPurchase,
            MonthlyPurchase = monthlyPurchase,

            GrossProfit = monthProfit.Summary.KnownGrossProfit,
            GrossProfitComplete = monthProfit.Summary.CostComplete,
            GrossProfitIncompleteInvoices = monthProfit.Summary.IncompleteInvoices,
            GrossProfitLimitation = monthProfit.Summary.Limitation,

            TotalProducts = totalProducts,
            TotalCustomers = totalCustomers,
            TotalSuppliers = totalSuppliers,

            LowStockProducts = lowStockProducts,
            OutOfStockProducts = outOfStockProducts,
            OutstandingCollections = outstanding,
            ExpiringWarranties = expiring,
            ExpiringWarrantiesAvailable = expiringAvailable
        };
    }

    private async Task<(int Count, bool Available)> CountExpiringWarrantiesSafeAsync(DateTime today)
    {
        try
        {
            var (from, toExclusive) = WarrantyDates.DayWindow(
                today,
                today.AddDays(WarrantyDates.ExpiringSoonDays));

            var count = await _context.Warranties
                .AsNoTracking()
                .CountAsync(w => !w.IsVoided && w.EndDate >= from && w.EndDate < toExclusive);

            return (count, true);
        }
        catch (Exception ex) when (SqlServerErrors.IsMissingObject(ex))
        {
            _logger.LogWarning(
                ex,
                "Warranties are not on this database; dashboard totals still load without that metric.");
            return (0, false);
        }
    }

    private async Task<decimal> SumOutstandingCollectionsAsync()
    {
        var rows = await _context.Sales
            .AsNoTracking()
            .Where(s => s.PaymentTracked)
            .Select(s => new
            {
                s.TotalAmount,
                Paid = s.Payments.Where(p => !p.IsVoided).Sum(p => (decimal?)p.Amount) ?? 0,
                Returned = _context.SalesReturns.Where(r => r.SaleId == s.Id).Sum(r => (decimal?)r.TotalAmount) ?? 0,
                Refunded = _context.SalesReturns.Where(r => r.SaleId == s.Id).Sum(r => (decimal?)r.RefundAmount) ?? 0
            })
            .ToListAsync();

        return rows.Sum(s =>
            PaymentCalculator.Summarise(s.TotalAmount, s.Returned, s.Paid, s.Refunded, true).Outstanding);
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
    public async Task<List<ChartPointDto>> GetSalesChartAsync(int days = 7)
    {
        var startDate = DateTime.Today.AddDays(-(days - 1));

        var sales = await _context.Sales
            .AsNoTracking()
            .Where(x => x.SaleDate >= startDate)
            .Select(x => new { x.SaleDate, x.TotalAmount })
            .ToListAsync();

        var grouped = sales
            .GroupBy(x => x.SaleDate.Date)
            .Select(g => new
            {
                Date = g.Key,
                Amount = g.Sum(x => x.TotalAmount)
            })
            .ToList();

        var result = new List<ChartPointDto>();

        for (int i = 0; i < days; i++)
        {
            var currentDate = startDate.AddDays(i);

            var daySale = grouped.FirstOrDefault(x => x.Date == currentDate);

            result.Add(new ChartPointDto
            {
                Label = currentDate.ToString("dd MMM"),
                Value = daySale?.Amount ?? 0
            });
        }

        return result;
    }
    public async Task<List<ChartPointDto>> GetPurchaseChartAsync(int days = 7)
    {
        var startDate = DateTime.Today.AddDays(-(days - 1));

        var purchases = await _context.Purchases
            .AsNoTracking()
            .Where(x => x.PurchaseDate >= startDate)
            .Select(x => new { x.PurchaseDate, x.TotalAmount })
            .ToListAsync();

        var grouped = purchases
            .GroupBy(x => x.PurchaseDate.Date)
            .Select(g => new
            {
                Date = g.Key,
                Amount = g.Sum(x => x.TotalAmount)
            })
            .ToList();

        var result = new List<ChartPointDto>();

        for (int i = 0; i < days; i++)
        {
            var currentDate = startDate.AddDays(i);

            var dayPurchase = grouped.FirstOrDefault(x => x.Date == currentDate);

            result.Add(new ChartPointDto
            {
                Label = currentDate.ToString("dd MMM"),
                Value = dayPurchase?.Amount ?? 0
            });
        }

        return result;
    }
}