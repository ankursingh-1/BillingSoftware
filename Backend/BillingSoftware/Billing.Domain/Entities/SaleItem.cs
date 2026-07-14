using Billing.Domain.Common;

namespace Billing.Domain.Entities;

public class SaleItem : BaseEntity
{
    public int SaleId { get; set; }
    public int ProductId { get; set; }
    public int Quantity { get; set; }
    public decimal SellingPrice { get; set; }
    public decimal Total { get; set; }
    public Sale Sale { get; set; } = null!;
    public Product Product { get; set; } = null!;
}