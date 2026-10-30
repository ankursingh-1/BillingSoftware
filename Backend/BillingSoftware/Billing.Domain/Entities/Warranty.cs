using Billing.Domain.Common;
using Billing.Domain.Enums;

namespace Billing.Domain.Entities;

/// <summary>
/// Frozen warranty registration for one sold unit (or one sale line when there
/// is no serial). Changing the product's default warranty later never rewrites
/// these dates.
/// </summary>
public class Warranty : AuditableEntity
{
    public string WarrantyNumber { get; set; } = string.Empty;

    public int SaleId { get; set; }
    public int SaleItemId { get; set; }
    public int ProductId { get; set; }
    public int? CustomerId { get; set; }

    public string InvoiceNumber { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public string? ProductSku { get; set; }

    public string CustomerName { get; set; } = string.Empty;
    public string CustomerMobile { get; set; } = string.Empty;

    /// <summary>Optional serial of this unit. Not serial-level inventory.</summary>
    public string? SerialNumber { get; set; }

    public int DurationValue { get; set; }
    public WarrantyDurationUnit DurationUnit { get; set; }

    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }

    public string? Notes { get; set; }
    public bool IsVoided { get; set; }

    public Sale Sale { get; set; } = null!;
    public SaleItem SaleItem { get; set; } = null!;
    public Product Product { get; set; } = null!;
    public Customer? Customer { get; set; }

    public ICollection<WarrantyClaim> Claims { get; set; } = new List<WarrantyClaim>();
}
