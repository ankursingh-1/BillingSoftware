namespace Billing.Application.DTOs.Sales
{
    public class CreateSaleRequest
    {
        public int CustomerId { get; set; }
        public List<CreateSaleItemRequest> Items { get; set; } = new();
    }
}