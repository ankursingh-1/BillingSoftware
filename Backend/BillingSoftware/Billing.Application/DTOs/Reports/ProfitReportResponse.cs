namespace Billing.Application.DTOs.Reports;

public class ProfitReportResponse
{
    public decimal TotalSales { get; set; }

    /// <summary>Sum of snapshot cost on sale lines that have CostPrice.</summary>
    public decimal CostOfGoodsSold { get; set; }

    public decimal? GrossProfit { get; set; }
    public decimal? ProfitPercentage { get; set; }

    public int LinesWithCost { get; set; }
    public int LinesMissingCost { get; set; }
    public bool CostComplete { get; set; }

    /// <summary>
    /// Set when CostPrice is missing on some lines. Gross profit is then omitted
    /// rather than approximated as sales minus purchases.
    /// </summary>
    public string? Limitation { get; set; }

    public List<ProfitReportItemDto> Items { get; set; } = new();
}