using Billing.Domain.Common;

namespace Billing.Domain.Entities;

public class Purchase : AuditableEntity
{
    public string PurchaseNumber { get; set; } = string.Empty;

    /// <summary>The supplier's own bill/invoice number (needed for input tax credit).</summary>
    public string? SupplierInvoiceNumber { get; set; }

    public int SupplierId { get; set; }
    public DateTime PurchaseDate { get; set; }

    /// <summary>Grand total of the purchase (taxable value + tax + round-off).</summary>
    public decimal TotalAmount { get; set; }

    public string? Remarks { get; set; }

    // ---- Tax snapshot ----
    public decimal Subtotal { get; set; }
    public decimal CgstTotal { get; set; }
    public decimal SgstTotal { get; set; }
    public decimal IgstTotal { get; set; }
    public decimal TaxTotal { get; set; }
    public decimal RoundOff { get; set; }

    /// <summary>False for purchases created before tax was recorded (tax unknown).</summary>
    public bool TaxCalculated { get; set; }
    public bool IsInterState { get; set; }
    public string? SupplierGstin { get; set; }

    public Guid? ClientRequestId { get; set; }

    public Supplier Supplier { get; set; } = null!;

    public ICollection<PurchaseItem> PurchaseItems { get; set; }
        = new List<PurchaseItem>();
}
