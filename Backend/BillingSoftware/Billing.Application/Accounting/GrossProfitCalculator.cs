namespace Billing.Application.Accounting;

/// <summary>One invoice line as needed to compute gross profit.</summary>
public readonly record struct ProfitLineFact(
    int Quantity,
    int ReturnedQuantity,
    decimal TaxExclusiveAmount,
    decimal? CostPrice);

/// <summary>Gross profit of one invoice after returns, tax excluded from revenue.</summary>
public readonly record struct InvoiceProfit(
    decimal NetRevenue,
    decimal? CostOfGoodsSold,
    decimal? Profit,
    bool CostComplete,
    bool HasUnknownCost,
    string CostStatus);

/// <summary>Period totals. Known profit is complete invoices only.</summary>
public readonly record struct ProfitSummary(
    decimal NetRevenue,
    decimal? KnownGrossProfit,
    decimal KnownCostOfGoodsSold,
    int Invoices,
    int CompleteInvoices,
    int IncompleteInvoices,
    int LinesWithCost,
    int LinesMissingCost,
    decimal CoveragePercent,
    bool CostComplete,
    string? Limitation);

/// <summary>
/// Single place Dashboard and Reports take gross profit from.
/// Revenue is tax-exclusive net of returns. Cost is the sale-line snapshot
/// when it is greater than zero, otherwise the last purchase on or before
/// the sale. Missing cost is never treated as zero. Incomplete invoices
/// are excluded from known gross profit (not mixed into a fake total).
/// </summary>
public static class GrossProfitCalculator
{
    public const string UnavailableStatus = "Cost unavailable";
    public const string PartialStatus = "Partial";
    public const string CompleteStatus = "Complete";

    public static InvoiceProfit ForInvoice(IEnumerable<ProfitLineFact> lines)
    {
        decimal netRevenue = 0;
        decimal cogs = 0;
        var anyUnknownSold = false;
        var anyKnownSold = false;

        foreach (var line in lines)
        {
            var sold = Math.Max(0, line.Quantity);
            var returned = Math.Clamp(line.ReturnedQuantity, 0, sold);
            var remaining = sold - returned;
            var revenue = NetRevenue(line.TaxExclusiveAmount, sold, remaining);
            netRevenue += revenue;

            if (remaining == 0)
                continue;

            if (line.CostPrice is { } cost && cost > 0)
            {
                cogs += GstCalculator.Round(remaining * cost);
                anyKnownSold = true;
            }
            else
            {
                anyUnknownSold = true;
            }
        }

        netRevenue = GstCalculator.Round(netRevenue);
        cogs = GstCalculator.Round(cogs);

        var complete = !anyUnknownSold;
        var status = complete
            ? CompleteStatus
            : anyKnownSold
                ? PartialStatus
                : UnavailableStatus;

        return new InvoiceProfit(
            netRevenue,
            complete ? cogs : null,
            complete ? GstCalculator.Round(netRevenue - cogs) : null,
            complete,
            anyUnknownSold,
            status);
    }

    public static ProfitSummary Summarise(IEnumerable<InvoiceProfit> invoices)
    {
        var list = invoices.ToList();
        var net = GstCalculator.Round(list.Sum(i => i.NetRevenue));
        var complete = list.Where(i => i.CostComplete).ToList();
        var incomplete = list.Count - complete.Count;
        var knownCogs = GstCalculator.Round(complete.Sum(i => i.CostOfGoodsSold ?? 0));
        decimal? knownProfit = complete.Count == 0 && incomplete > 0
            ? null
            : GstCalculator.Round(complete.Sum(i => i.Profit ?? 0));

        var coverage = list.Count == 0
            ? 100m
            : GstCalculator.Round(100m * complete.Count / list.Count);

        string? limitation = null;
        if (incomplete > 0)
        {
            limitation =
                "Known gross profit is only for invoices whose remaining sold lines all have a known cost " +
                "(the sale-line snapshot, or the last purchase on or before the sale). " +
                $"{incomplete} invoice(s) have cost unavailable and are excluded from that figure. " +
                "It is not calculated as sales minus purchases.";
        }

        return new ProfitSummary(
            net,
            knownProfit,
            knownCogs,
            list.Count,
            complete.Count,
            incomplete,
            0,
            0,
            coverage,
            incomplete == 0,
            limitation);
    }

    public static ProfitSummary Summarise(
        IEnumerable<InvoiceProfit> invoices,
        int linesWithCost,
        int linesMissingCost)
    {
        var summary = Summarise(invoices);
        return summary with
        {
            LinesWithCost = linesWithCost,
            LinesMissingCost = linesMissingCost
        };
    }

    private static decimal NetRevenue(decimal taxExclusiveAmount, int soldQty, int remainingQty)
    {
        if (soldQty <= 0 || remainingQty <= 0)
            return 0;

        if (remainingQty == soldQty)
            return GstCalculator.Round(taxExclusiveAmount);

        return GstCalculator.Round(taxExclusiveAmount * remainingQty / soldQty);
    }
}
