namespace Billing.Application.DTOs.Dashboard
{
    public class RecentPurchaseDto
    {
        public int PurchaseId { get; set; }
        public string PurchaseNumber { get; set; } = string.Empty;
        public string SupplierName { get; set; } = string.Empty;
        public decimal TotalAmount { get; set; }
        public DateTime PurchaseDate { get; set; }
    }
}