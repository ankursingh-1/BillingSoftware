using Billing.Application.Accounting;
using Billing.Application.DTOs.Expense;
using Billing.Application.DTOs.Reports;
using Billing.Application.Inventory;
using Billing.Persistence.Context;
using Microsoft.EntityFrameworkCore;

namespace Billing.API.Services;

public class ReportService
{
    private readonly BillingDbContext _context;
    private readonly StockAlertPolicy _alerts;
    private readonly GrossProfitService _grossProfit;

    public ReportService(BillingDbContext context, StockAlertPolicy alerts, GrossProfitService grossProfit)
    {
        _context = context;
        _alerts = alerts;
        _grossProfit = grossProfit;
    }

    public async Task<SalesReportResponse> GetSalesReportAsync(SalesReportRequest request)
    {
        var query = _context.Sales
            .AsNoTracking()
            .Include(x => x.Customer)
            .AsQueryable();

        if (request.FromDate.HasValue)
        {
            var from = ReportWindow.StartOfDay(request.FromDate.Value);
            query = query.Where(x => x.SaleDate >= from);
        }

        if (request.ToDate.HasValue)
        {
            var toExclusive = ReportWindow.EndExclusive(request.ToDate.Value);
            query = query.Where(x => x.SaleDate < toExclusive);
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

        var (pageNumber, pageSize) = NormalizePage(request.PageNumber, request.PageSize);
        var totalRecords = await query.CountAsync();

        var grandTotal = await query.SumAsync(x => x.TotalAmount);

        var items = await query
            .OrderByDescending(x => x.SaleDate)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
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
            PageNumber = pageNumber,
            PageSize = pageSize,
            TotalRecords = totalRecords,
            TotalPages = (int)Math.Ceiling((double)totalRecords / pageSize),
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
        {
            var from = ReportWindow.StartOfDay(request.FromDate.Value);
            query = query.Where(x => x.PurchaseDate >= from);
        }

        if (request.ToDate.HasValue)
        {
            var toExclusive = ReportWindow.EndExclusive(request.ToDate.Value);
            query = query.Where(x => x.PurchaseDate < toExclusive);
        }

        if (request.SupplierId.HasValue)
            query = query.Where(x => x.SupplierId == request.SupplierId.Value);

        if (!string.IsNullOrWhiteSpace(request.PurchaseNumber))
            query = query.Where(x => x.PurchaseNumber.Contains(request.PurchaseNumber));

        var (pageNumber, pageSize) = NormalizePage(request.PageNumber, request.PageSize);
        var totalRecords = await query.CountAsync();

        var grandTotal = await query.SumAsync(x => x.TotalAmount);

        var items = await query
            .OrderByDescending(x => x.PurchaseDate)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
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
            PageNumber = pageNumber,
            PageSize = pageSize,
            TotalRecords = totalRecords,
            TotalPages = (int)Math.Ceiling((double)totalRecords / pageSize),
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
            var threshold = _alerts.LowStockThreshold;
            query = query.Where(x => x.Stock > 0 && x.Stock <= threshold);
        }

        if (request.OutOfStockOnly == true)
        {
            query = query.Where(x => x.Stock == 0);
        }

        var (pageNumber, pageSize) = NormalizePage(request.PageNumber, request.PageSize);
        var totalRecords = await query.CountAsync();

        var totalStockValue = await query.SumAsync(x => x.Stock * x.PurchasePrice);

        var items = await query
            .OrderBy(x => x.Name)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
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
            PageNumber = pageNumber,
            PageSize = pageSize,
            TotalRecords = totalRecords,
            TotalPages = (int)Math.Ceiling((double)totalRecords / pageSize),
            TotalStockValue = totalStockValue,
            Items = items
        };
    }

    public async Task<ProfitReportResponse> GetProfitReportAsync(ProfitReportRequest request)
    {
        DateTime? from = request.FromDate.HasValue
            ? ReportWindow.StartOfDay(request.FromDate.Value)
            : null;
        DateTime? toExclusive = request.ToDate.HasValue
            ? ReportWindow.EndExclusive(request.ToDate.Value)
            : null;

        var period = await _grossProfit.ForPeriodAsync(from, toExclusive);
        var summary = period.Summary;
        var completeRevenue = period.Invoices.Where(i => i.CostComplete).Sum(i => i.SaleAmount);
        decimal? margin = summary.KnownGrossProfit is { } profit && completeRevenue > 0
            ? Math.Round(profit / completeRevenue * 100, 2, MidpointRounding.AwayFromZero)
            : null;

        return new ProfitReportResponse
        {
            TotalSales = summary.NetRevenue,
            CostOfGoodsSold = summary.KnownCostOfGoodsSold,
            GrossProfit = summary.KnownGrossProfit,
            ProfitPercentage = margin,
            CompleteInvoices = summary.CompleteInvoices,
            IncompleteInvoices = summary.IncompleteInvoices,
            CoveragePercent = summary.CoveragePercent,
            LinesWithCost = summary.LinesWithCost,
            LinesMissingCost = summary.LinesMissingCost,
            CostComplete = summary.CostComplete,
            Limitation = summary.Limitation,
            Items = period.Invoices.ToList()
        };
    }

    public async Task<ExpenseReportResponse> GetExpenseReportAsync(ExpenseSearchRequest request)
    {
        var result = await new ExpenseService(_context).SearchAsync(request);
        var total = await _context.Expenses.AsNoTracking()
            .Where(x => !x.IsDeleted)
            .Where(x => !request.FromDate.HasValue || x.ExpenseDate >= request.FromDate.Value.Date)
            .Where(x => !request.ToDate.HasValue || x.ExpenseDate <= request.ToDate.Value.Date)
            .SumAsync(x => (decimal?)x.Amount) ?? 0;

        return new ExpenseReportResponse
        {
            PageNumber = result.PageNumber,
            PageSize = result.PageSize,
            TotalRecords = result.TotalRecords,
            TotalPages = result.TotalPages,
            GrandTotal = total,
            Items = result.Items
        };
    }

    public async Task<CustomerReportResponse> GetCustomerReportAsync(PartyReportRequest request)
    {
        var pageSize = Math.Clamp(request.PageSize <= 0 ? 10 : request.PageSize, 1, 10_000);
        var pageNumber = Math.Max(request.PageNumber, 1);

        var sales = _context.Sales.AsNoTracking().AsQueryable();
        if (request.FromDate.HasValue)
            sales = sales.Where(s => s.SaleDate >= request.FromDate.Value.Date);
        if (request.ToDate.HasValue)
            sales = sales.Where(s => s.SaleDate < request.ToDate.Value.Date.AddDays(1));

        var saleRows = await sales.Select(s => new
        {
            s.CustomerId,
            s.TotalAmount,
            s.PaymentTracked,
            Paid = s.Payments.Where(p => !p.IsVoided).Sum(p => (decimal?)p.Amount) ?? 0,
            Returned = _context.SalesReturns.Where(r => r.SaleId == s.Id).Sum(r => (decimal?)r.TotalAmount) ?? 0,
            Refunded = _context.SalesReturns.Where(r => r.SaleId == s.Id).Sum(r => (decimal?)r.RefundAmount) ?? 0
        }).ToListAsync();

        var byCustomer = saleRows.GroupBy(s => s.CustomerId).ToDictionary(
            g => g.Key,
            g => g.ToList());

        var customers = _context.Customers.AsNoTracking().Where(c => !c.IsDeleted);
        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var search = request.Search.Trim();
            customers = customers.Where(c =>
                c.Name.Contains(search) ||
                c.Mobile.Contains(search) ||
                (c.Gstin != null && c.Gstin.Contains(search)));
        }

        var list = await customers.OrderBy(c => c.Name).ToListAsync();
        var items = list.Select(c =>
        {
            byCustomer.TryGetValue(c.Id, out var group);
            group ??= [];
            var billed = group.Sum(s => s.TotalAmount);
            var outstanding = group
                .Where(s => s.PaymentTracked)
                .Sum(s => PaymentCalculator.Summarise(
                    s.TotalAmount, s.Returned, s.Paid, s.Refunded, true).Outstanding);

            return new CustomerReportItemDto
            {
                CustomerId = c.Id,
                Name = c.Name,
                Mobile = c.Mobile,
                Gstin = c.Gstin,
                InvoiceCount = group.Count,
                Billed = billed,
                Outstanding = outstanding
            };
        }).ToList();

        var totalBilled = items.Sum(i => i.Billed);
        var totalOutstanding = items.Sum(i => i.Outstanding);
        var totalRecords = items.Count;
        var page = items.Skip((pageNumber - 1) * pageSize).Take(pageSize).ToList();

        return new CustomerReportResponse
        {
            PageNumber = pageNumber,
            PageSize = pageSize,
            TotalRecords = totalRecords,
            TotalPages = (int)Math.Ceiling(totalRecords / (double)pageSize),
            TotalBilled = totalBilled,
            TotalOutstanding = totalOutstanding,
            Items = page
        };
    }

    public async Task<SupplierReportResponse> GetSupplierReportAsync(PartyReportRequest request)
    {
        var pageSize = Math.Clamp(request.PageSize <= 0 ? 10 : request.PageSize, 1, 10_000);
        var pageNumber = Math.Max(request.PageNumber, 1);

        var purchases = _context.Purchases.AsNoTracking().AsQueryable();
        if (request.FromDate.HasValue)
            purchases = purchases.Where(p => p.PurchaseDate >= request.FromDate.Value.Date);
        if (request.ToDate.HasValue)
            purchases = purchases.Where(p => p.PurchaseDate < request.ToDate.Value.Date.AddDays(1));

        var purchaseRows = await purchases
            .GroupBy(p => p.SupplierId)
            .Select(g => new
            {
                SupplierId = g.Key,
                Count = g.Count(),
                Total = g.Sum(p => p.TotalAmount)
            })
            .ToListAsync();

        var bySupplier = purchaseRows.ToDictionary(x => x.SupplierId);

        var suppliers = _context.Suppliers.AsNoTracking().Where(s => !s.IsDeleted);
        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var search = request.Search.Trim();
            suppliers = suppliers.Where(s =>
                s.Name.Contains(search) ||
                s.CompanyName.Contains(search) ||
                s.Mobile.Contains(search) ||
                (s.Gstin != null && s.Gstin.Contains(search)));
        }

        var list = await suppliers.OrderBy(s => s.CompanyName).ThenBy(s => s.Name).ToListAsync();
        var items = list.Select(s =>
        {
            bySupplier.TryGetValue(s.Id, out var group);
            return new SupplierReportItemDto
            {
                SupplierId = s.Id,
                Name = s.Name,
                CompanyName = s.CompanyName,
                Mobile = s.Mobile,
                Gstin = s.Gstin,
                PurchaseCount = group?.Count ?? 0,
                Purchased = group?.Total ?? 0
            };
        }).ToList();

        var totalPurchased = items.Sum(i => i.Purchased);
        var totalRecords = items.Count;
        var page = items.Skip((pageNumber - 1) * pageSize).Take(pageSize).ToList();

        return new SupplierReportResponse
        {
            PageNumber = pageNumber,
            PageSize = pageSize,
            TotalRecords = totalRecords,
            TotalPages = (int)Math.Ceiling(totalRecords / (double)pageSize),
            TotalPurchased = totalPurchased,
            Items = page
        };
    }

    public async Task<PaymentReportResponse> GetPaymentReportAsync(PaymentReportRequest request)
    {
        var pageSize = Math.Clamp(request.PageSize <= 0 ? 10 : request.PageSize, 1, 10_000);
        var pageNumber = Math.Max(request.PageNumber, 1);

        var query = _context.Payments.AsNoTracking()
            .Where(p => !p.IsVoided);

        if (request.FromDate.HasValue)
            query = query.Where(p => p.PaymentDate >= request.FromDate.Value.Date);
        if (request.ToDate.HasValue)
            query = query.Where(p => p.PaymentDate < request.ToDate.Value.Date.AddDays(1));

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var search = request.Search.Trim();
            query = query.Where(p =>
                p.Sale.InvoiceNumber.Contains(search) ||
                p.Sale.Customer.Name.Contains(search) ||
                (p.ReceiptNumber != null && p.ReceiptNumber.Contains(search)) ||
                (p.Reference != null && p.Reference.Contains(search)));
        }

        var totalRecords = await query.CountAsync();
        var totalCollected = await query.SumAsync(p => (decimal?)p.Amount) ?? 0;

        var items = await query
            .OrderByDescending(p => p.PaymentDate)
            .ThenByDescending(p => p.Id)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .Select(p => new PaymentReportItemDto
            {
                PaymentId = p.Id,
                ReceiptNumber = p.ReceiptNumber,
                InvoiceNumber = p.Sale.InvoiceNumber,
                CustomerName = p.Sale.Customer.Name,
                PaymentDate = p.PaymentDate,
                Method = p.Method.ToString(),
                Amount = p.Amount,
                Reference = p.Reference
            })
            .ToListAsync();

        return new PaymentReportResponse
        {
            PageNumber = pageNumber,
            PageSize = pageSize,
            TotalRecords = totalRecords,
            TotalPages = (int)Math.Ceiling(totalRecords / (double)pageSize),
            TotalCollected = totalCollected,
            Items = items
        };
    }

    private static (int PageNumber, int PageSize) NormalizePage(int pageNumber, int pageSize)
    {
        var size = Math.Clamp(pageSize <= 0 ? 10 : pageSize, 1, 10_000);
        return (Math.Max(pageNumber, 1), size);
    }
}