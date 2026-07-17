using Billing.Application.DTOs.Common;

namespace Billing.Application.DTOs.Reports;

public class PurchaseReportRequest : PaginationRequest
{
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
    public int? SupplierId { get; set; }
    public string? PurchaseNumber { get; set; }
}