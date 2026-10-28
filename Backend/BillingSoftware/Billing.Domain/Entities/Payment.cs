using Billing.Domain.Common;
using Billing.Domain.Enums;

namespace Billing.Domain.Entities;

/// <summary>
/// Money received against a sale. Payments are never deleted; a wrong entry
/// is voided with a reason so the history stays auditable.
/// </summary>
public class Payment : AuditableEntity
{
    public int SaleId { get; set; }
    public Sale Sale { get; set; } = null!;

    /// <summary>Receipt number (RCT-2627-000001). Null only for payments made before receipts existed.</summary>
    public string? ReceiptNumber { get; set; }

    public decimal Amount { get; set; }
    public DateTime PaymentDate { get; set; }
    public PaymentMethod Method { get; set; }
    public string? Reference { get; set; }
    public string? Notes { get; set; }

    public bool IsVoided { get; set; }
    public DateTime? VoidedOn { get; set; }
    public string? VoidedBy { get; set; }
    public string? VoidReason { get; set; }
}
