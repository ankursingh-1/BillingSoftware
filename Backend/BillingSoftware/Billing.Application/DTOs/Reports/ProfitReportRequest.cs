namespace Billing.Application.DTOs.Reports;

public class ProfitReportRequest
{
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
}