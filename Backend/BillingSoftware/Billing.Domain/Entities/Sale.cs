using Billing.Domain.Common;

namespace Billing.Domain.Entities;

public class Sale : AuditableEntity
{
    public string InvoiceNumber { get; set; } = string.Empty;
    public int CustomerId { get; set; }
    public DateTime SaleDate { get; set; }
    public decimal TotalAmount { get; set; }
    public string? Remarks { get; set; }
    public Customer Customer { get; set; } = null!;
    public ICollection<SaleItem> SaleItems { get; set; }
        = new List<SaleItem>();
}