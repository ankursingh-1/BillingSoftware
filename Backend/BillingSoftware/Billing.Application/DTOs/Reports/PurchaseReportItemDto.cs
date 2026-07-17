namespace Billing.Application.DTOs.Reports;

public class PurchaseReportItemDto
{
    public int PurchaseId { get; set; }
    public string PurchaseNumber { get; set; } = string.Empty;
    public string SupplierName { get; set; } = string.Empty;
    public DateTime PurchaseDate { get; set; }
    public decimal TotalAmount { get; set; }
}