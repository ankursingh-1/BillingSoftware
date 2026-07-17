using Billing.Application.DTOs.Common;
namespace Billing.Application.DTOs.Reports;
public class StockReportResponse : PagedResult<StockReportItemDto>
{
    public decimal TotalStockValue { get; set; }
}