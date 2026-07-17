using Billing.Application.DTOs.Common;

namespace Billing.Application.DTOs.Reports;

public class StockReportRequest : PaginationRequest
{
    public string? Search { get; set; }
    public bool? LowStockOnly { get; set; }
    public bool? OutOfStockOnly { get; set; }
}