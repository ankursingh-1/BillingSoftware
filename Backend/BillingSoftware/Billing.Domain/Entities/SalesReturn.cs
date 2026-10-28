using Billing.Domain.Common;
using Billing.Domain.Enums;

namespace Billing.Domain.Entities;

/// <summary>Credit note: goods returned by a customer against a sale.</summary>
public class SalesReturn : AuditableEntity
{
    public string ReturnNumber { get; set; } = string.Empty;
    public int SaleId { get; set; }
    public Sale Sale { get; set; } = null!;
    public DateTime ReturnDate { get; set; }
    public string? Reason { get; set; }

    public decimal Subtotal { get; set; }
    public decimal CgstTotal { get; set; }
    public decimal SgstTotal { get; set; }
    public decimal IgstTotal { get; set; }
    public decimal TaxTotal { get; set; }

    /// <summary>Credit value of the return (taxable value + tax).</summary>
    public decimal TotalAmount { get; set; }

    /// <summary>Cash actually handed back to the customer with this return.</summary>
    public decimal RefundAmount { get; set; }
    public PaymentMethod? RefundMethod { get; set; }
    public string? Remarks { get; set; }

    public ICollection<SalesReturnItem> Items { get; set; } = new List<SalesReturnItem>();
}
