using Billing.Application.DTOs.Common;
using System.ComponentModel.DataAnnotations;

namespace Billing.Application.DTOs.Reports;

public class SalesReportRequest : PaginationRequest
{
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
    public int? CustomerId { get; set; }
    [StringLength(50, ErrorMessage = "Invoice number cannot exceed 50 characters.")]
    public string? InvoiceNumber { get; set; }
}