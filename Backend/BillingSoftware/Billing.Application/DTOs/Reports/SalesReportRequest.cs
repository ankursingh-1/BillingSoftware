using Billing.Application.DTOs.Common;

namespace Billing.Application.DTOs.Reports;

public class SalesReportRequest : PaginationRequest
{
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
    public int? CustomerId { get; set; }
    public string? InvoiceNumber { get; set; }
}