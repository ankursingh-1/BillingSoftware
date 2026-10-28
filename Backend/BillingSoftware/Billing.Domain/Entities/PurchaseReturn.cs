using Billing.Domain.Common;
using Billing.Domain.Enums;

namespace Billing.Domain.Entities;

/// <summary>Debit note: goods sent back to a supplier against a purchase.</summary>
public class PurchaseReturn : AuditableEntity
{
    public string ReturnNumber { get; set; } = string.Empty;
    public int PurchaseId { get; set; }
    public Purchase Purchase { get; set; } = null!;
    public DateTime ReturnDate { get; set; }
    public string? Reason { get; set; }

    public decimal Subtotal { get; set; }
    public decimal CgstTotal { get; set; }
    public decimal SgstTotal { get; set; }
    public decimal IgstTotal { get; set; }
    public decimal TaxTotal { get; set; }
    public decimal TotalAmount { get; set; }

    /// <summary>Money received back from the supplier (0 if settled as a credit/adjustment).</summary>
    public decimal RefundReceived { get; set; }
    public PaymentMethod? RefundMethod { get; set; }

    /// <summary>Free-text supplier adjustment detail (credit note number, etc.).</summary>
    public string? AdjustmentNote { get; set; }

    public ICollection<PurchaseReturnItem> Items { get; set; } = new List<PurchaseReturnItem>();
}
