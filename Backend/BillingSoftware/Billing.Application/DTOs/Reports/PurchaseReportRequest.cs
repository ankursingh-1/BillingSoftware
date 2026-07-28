using Billing.Application.DTOs.Common;
using System.ComponentModel.DataAnnotations;

namespace Billing.Application.DTOs.Reports;

public class PurchaseReportRequest : PaginationRequest
{
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
    public int? SupplierId { get; set; }
    [StringLength(50, ErrorMessage = "Purchase number cannot exceed 50 characters.")]
    public string? PurchaseNumber { get; set; }
}