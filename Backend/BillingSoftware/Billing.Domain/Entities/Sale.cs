using Billing.Domain.Common;

namespace Billing.Domain.Entities;

public class Sale : AuditableEntity
{
    public string InvoiceNumber { get; set; } = string.Empty;
    public int CustomerId { get; set; }
    public DateTime SaleDate { get; set; }

    /// <summary>
    /// Grand total of the invoice (taxable value + tax + round-off).
    /// Kept under its original name so reports and the dashboard keep working.
    /// </summary>
    public decimal TotalAmount { get; set; }

    public string? Remarks { get; set; }

    // ---- Tax snapshot (frozen when the invoice is created) ----

    /// <summary>Sum of line taxable amounts (before tax).</summary>
    public decimal Subtotal { get; set; }
    public decimal CgstTotal { get; set; }
    public decimal SgstTotal { get; set; }
    public decimal IgstTotal { get; set; }
    public decimal TaxTotal { get; set; }

    /// <summary>Adjustment that rounds the invoice to the nearest rupee.</summary>
    public decimal RoundOff { get; set; }

    /// <summary>
    /// False for sales that were created before GST snapshots existed:
    /// their tax is unknown (not "zero"), so it is never reported as zero tax.
    /// </summary>
    public bool TaxCalculated { get; set; }

    public bool IsInterState { get; set; }
    public string? PlaceOfSupplyStateCode { get; set; }
    public string? CustomerGstin { get; set; }
    public string? CompanyGstin { get; set; }
    public string? CompanyStateCode { get; set; }

    /// <summary>
    /// False for sales created before payments were recorded: their payment
    /// state is unknown ("not recorded"), not "unpaid".
    /// </summary>
    public bool PaymentTracked { get; set; }

    /// <summary>Client-generated id that makes a retried/double-clicked submit idempotent.</summary>
    public Guid? ClientRequestId { get; set; }

    /// <summary>
    /// JSON copy of the company and customer details as they were when the invoice was
    /// created, so editing the company or customer later never changes an issued invoice.
    /// Null for invoices created before snapshots existed (they use the current details).
    /// </summary>
    public string? DocumentSnapshot { get; set; }

    public Customer Customer { get; set; } = null!;

    public ICollection<SaleItem> SaleItems { get; set; }
        = new List<SaleItem>();

    public ICollection<Payment> Payments { get; set; }
        = new List<Payment>();
}
