using Billing.Application.DTOs.Common;

namespace Billing.Application.DTOs.Reports;

public class PurchaseReportResponse : PagedResult<PurchaseReportItemDto>
{
    public decimal GrandTotal { get; set; }
}
