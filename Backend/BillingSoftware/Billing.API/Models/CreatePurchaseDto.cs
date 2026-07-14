namespace Billing.API.Models;

public class CreatePurchaseDto
{
    public int SupplierId { get; set; }
    public DateTime PurchaseDate { get; set; }
    public string? Remarks { get; set; }
    public List<PurchaseItemDto> Items { get; set; } = new();
}