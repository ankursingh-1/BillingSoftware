namespace Billing.Application.DTOs.Dashboard
{
    public class LowStockDto
    {
        public int ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public int CurrentStock { get; set; }
    }
}