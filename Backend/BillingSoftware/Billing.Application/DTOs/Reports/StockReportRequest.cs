using Billing.Application.DTOs.Common;
using System.ComponentModel.DataAnnotations;

namespace Billing.Application.DTOs.Reports;

public class StockReportRequest : PaginationRequest
{
    [StringLength(100, ErrorMessage = "Search cannot exceed 100 characters.")]
    public string? Search { get; set; }
    public bool? LowStockOnly { get; set; }
    public bool? OutOfStockOnly { get; set; }
}