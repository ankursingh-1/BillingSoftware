namespace Billing.Application.DTOs.Dashboard
{
    public class RecentSaleDto
    {
        public int SaleId { get; set; }
        public string InvoiceNumber { get; set; } = string.Empty;
        public string CustomerName { get; set; } = string.Empty;
        public decimal TotalAmount { get; set; }
        public DateTime SaleDate { get; set; }
    }
}