using Billing.Application.DTOs.Common;

namespace Billing.Application.DTOs.StockAdjustment
{
    public class StockAdjustmentDto
    {
        public int Id { get; set; }
        public string? ReferenceNo { get; set; }
        public int ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public string? Sku { get; set; }
        public string Reason { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public bool IncreaseStock { get; set; }
        public int PreviousStock { get; set; }
        public int CurrentStock { get; set; }
        public string? Remarks { get; set; }
        public string? CreatedBy { get; set; }
        public DateTime AdjustmentDate { get; set; }
    }

    public class StockAdjustmentResult
    {
        public string ReferenceNo { get; set; } = string.Empty;
        public int ProductId { get; set; }
        public int PreviousStock { get; set; }
        public int CurrentStock { get; set; }
    }

    public class StockAdjustmentSearchRequest : PaginationRequest
    {
        public string? Search { get; set; }
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
    }
}