using Billing.Domain.Common;

namespace Billing.Domain.Entities;

public class SalesReturnItem : BaseEntity
{
    public int SalesReturnId { get; set; }
    public SalesReturn SalesReturn { get; set; } = null!;
    public int SaleItemId { get; set; }
    public SaleItem SaleItem { get; set; } = null!;
    public int ProductId { get; set; }
    public Product Product { get; set; } = null!;

    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal TaxableAmount { get; set; }
    public decimal TaxRate { get; set; }
    public decimal CgstAmount { get; set; }
    public decimal SgstAmount { get; set; }
    public decimal IgstAmount { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal Total { get; set; }
}
