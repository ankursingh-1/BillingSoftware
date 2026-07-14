namespace Billing.API.Models;

public class PurchaseItemDto
{
    public int ProductId { get; set; }
    public int Quantity { get; set; }
    public decimal PurchasePrice { get; set; }
}