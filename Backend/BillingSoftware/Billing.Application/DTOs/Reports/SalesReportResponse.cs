using Billing.Application.DTOs.Common;

namespace Billing.Application.DTOs.Reports;

public class SalesReportResponse : PagedResult<SalesReportItemDto>
{
    public decimal GrandTotal { get; set; }
}