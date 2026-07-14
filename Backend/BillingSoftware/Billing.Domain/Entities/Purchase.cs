using Billing.Domain.Common;

namespace Billing.Domain.Entities;

public class Purchase : AuditableEntity
{
    public string PurchaseNumber { get; set; } = string.Empty;
    public int SupplierId { get; set; }
    public DateTime PurchaseDate { get; set; }
    public decimal TotalAmount { get; set; }
    public string? Remarks { get; set; }
    public Supplier Supplier { get; set; } = null!;
    public ICollection<PurchaseItem> PurchaseItems { get; set; }
        = new List<PurchaseItem>();
}