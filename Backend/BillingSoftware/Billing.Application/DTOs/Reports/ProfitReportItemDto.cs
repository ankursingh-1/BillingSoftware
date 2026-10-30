namespace Billing.Application.DTOs.Reports;

public class ProfitReportItemDto
{
    public int SaleId { get; set; }
    public string InvoiceNumber { get; set; } = string.Empty;
    public DateTime SaleDate { get; set; }
    /// <summary>Tax-exclusive net revenue after returns.</summary>
    public decimal SaleAmount { get; set; }
    public decimal? CostOfGoodsSold { get; set; }
    public decimal? Profit { get; set; }
    public bool CostComplete { get; set; }
    public string CostStatus { get; set; } = "Complete";
}