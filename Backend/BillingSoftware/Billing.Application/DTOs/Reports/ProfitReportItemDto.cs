namespace Billing.Application.DTOs.Reports;

public class ProfitReportItemDto
{
    public string InvoiceNumber { get; set; } = string.Empty;
    public DateTime SaleDate { get; set; }
    public decimal SaleAmount { get; set; }
    public decimal? CostOfGoodsSold { get; set; }
    public decimal? Profit { get; set; }
    public bool CostComplete { get; set; }
}