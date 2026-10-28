namespace Billing.Application.DTOs.Sales
{
    public class SaleResponse
    {
        public int SaleId { get; set; }
        public string InvoiceNumber { get; set; } = string.Empty;
        public decimal TotalAmount { get; set; }
        public decimal PaidAmount { get; set; }
        public decimal Outstanding { get; set; }
        public string PaymentStatus { get; set; } = string.Empty;

        /// <summary>True when this request had already been processed and the original sale is returned.</summary>
        public bool IsDuplicate { get; set; }
    }
}
