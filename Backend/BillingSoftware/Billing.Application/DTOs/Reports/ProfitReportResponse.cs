namespace Billing.Application.DTOs.Reports;

public class ProfitReportResponse
{
    public decimal TotalSales { get; set; }
    public decimal TotalPurchaseCost { get; set; }
    public decimal GrossProfit { get; set; }
    public decimal ProfitPercentage { get; set; }
    public List<ProfitReportItemDto> Items { get; set; } = new();
}