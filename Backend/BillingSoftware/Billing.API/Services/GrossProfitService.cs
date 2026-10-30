using Billing.Application.Accounting;
using Billing.Application.DTOs.Reports;
using Billing.Persistence.Context;
using Microsoft.EntityFrameworkCore;

namespace Billing.API.Services;

/// <summary>
/// Loads sale/return facts and runs <see cref="GrossProfitCalculator"/>.
/// Dashboard and Reports both use this so the figures cannot drift.
/// </summary>
public class GrossProfitService
{
    private readonly BillingDbContext _context;

    public GrossProfitService(BillingDbContext context)
    {
        _context = context;
    }

    public async Task<ProfitPeriod> ForPeriodAsync(DateTime? fromInclusive, DateTime? toExclusive)
    {
        var query = _context.Sales.AsNoTracking().Include(s => s.SaleItems).AsQueryable();

        if (fromInclusive.HasValue)
            query = query.Where(s => s.SaleDate >= fromInclusive.Value);
        if (toExclusive.HasValue)
            query = query.Where(s => s.SaleDate < toExclusive.Value);

        var sales = await query
            .OrderByDescending(s => s.SaleDate)
            .ThenByDescending(s => s.Id)
            .ToListAsync();

        var saleIds = sales.Select(s => s.Id).ToList();
        var returned = saleIds.Count == 0
            ? new Dictionary<int, int>()
            : await _context.SalesReturnItems.AsNoTracking()
                .Where(i => saleIds.Contains(i.SaleItem.SaleId))
                .GroupBy(i => i.SaleItemId)
                .Select(g => new { g.Key, Qty = g.Sum(x => x.Quantity) })
                .ToDictionaryAsync(x => x.Key, x => x.Qty);

        var productIds = sales.SelectMany(s => s.SaleItems.Select(i => i.ProductId)).Distinct().ToList();
        var buyRows = await _context.PurchaseItems.AsNoTracking()
            .Where(i => productIds.Contains(i.ProductId) && i.PurchasePrice > 0)
            .Select(i => new { i.ProductId, i.PurchasePrice, i.Purchase.PurchaseDate, i.Id })
            .ToListAsync();

        var purchasesByProduct = buyRows
            .GroupBy(r => r.ProductId)
            .ToDictionary(
                g => g.Key,
                g => (IReadOnlyList<(DateTime PurchaseDate, decimal Price)>)g
                    .OrderByDescending(r => r.PurchaseDate)
                    .ThenByDescending(r => r.Id)
                    .Select(r => (r.PurchaseDate, r.PurchasePrice))
                    .ToList());

        var invoices = new List<ProfitReportItemDto>(sales.Count);
        var profits = new List<InvoiceProfit>(sales.Count);
        var linesWithCost = 0;
        var linesMissing = 0;

        foreach (var sale in sales)
        {
            var facts = sale.SaleItems.Select(line =>
            {
                returned.TryGetValue(line.Id, out var returnedQty);
                purchasesByProduct.TryGetValue(line.ProductId, out var purchases);
                var cost = CostBackfill.ForProfit(
                    line.CostPrice,
                    sale.SaleDate,
                    purchases ?? Array.Empty<(DateTime, decimal)>());

                var remaining = Math.Max(0, line.Quantity - returnedQty);
                if (remaining > 0)
                {
                    if (cost.HasValue) linesWithCost++;
                    else linesMissing++;
                }

                return new ProfitLineFact(
                    line.Quantity,
                    returnedQty,
                    line.TaxableAmount ?? line.Total,
                    cost);
            }).ToList();

            var profit = GrossProfitCalculator.ForInvoice(facts);
            profits.Add(profit);
            invoices.Add(new ProfitReportItemDto
            {
                SaleId = sale.Id,
                InvoiceNumber = sale.InvoiceNumber,
                SaleDate = sale.SaleDate,
                SaleAmount = profit.NetRevenue,
                CostOfGoodsSold = profit.CostOfGoodsSold,
                Profit = profit.Profit,
                CostComplete = profit.CostComplete,
                CostStatus = profit.CostStatus
            });
        }

        var summary = GrossProfitCalculator.Summarise(profits, linesWithCost, linesMissing);
        return new ProfitPeriod(summary, invoices);
    }
}

public readonly record struct ProfitPeriod(
    ProfitSummary Summary,
    IReadOnlyList<ProfitReportItemDto> Invoices);
