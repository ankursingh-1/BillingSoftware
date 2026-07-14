namespace Billing.Application.DTOs.Sales
{
    public class SaleResponse
    {
        public int SaleId { get; set; }
        public string InvoiceNumber { get; set; } = string.Empty;
        public decimal TotalAmount { get; set; }
    }
}