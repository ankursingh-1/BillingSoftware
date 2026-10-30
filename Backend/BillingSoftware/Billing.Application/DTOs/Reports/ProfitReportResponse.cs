namespace Billing.Application.DTOs.Reports;

public class ProfitReportResponse
{
    /// <summary>Tax-exclusive net sales after returns for the whole period.</summary>
    public decimal TotalSales { get; set; }

    /// <summary>COGS of complete invoices only.</summary>
    public decimal CostOfGoodsSold { get; set; }

    /// <summary>
    /// Gross profit of invoices whose remaining sold lines all have a cost snapshot.
    /// Null when none of those invoices exist and some costs are missing (not zero).
    /// </summary>
    public decimal? GrossProfit { get; set; }

    public decimal? ProfitPercentage { get; set; }

    public int CompleteInvoices { get; set; }
    public int IncompleteInvoices { get; set; }
    public decimal CoveragePercent { get; set; }

    public int LinesWithCost { get; set; }
    public int LinesMissingCost { get; set; }
    public bool CostComplete { get; set; }

    public string? Limitation { get; set; }

    public List<ProfitReportItemDto> Items { get; set; } = new();
}
