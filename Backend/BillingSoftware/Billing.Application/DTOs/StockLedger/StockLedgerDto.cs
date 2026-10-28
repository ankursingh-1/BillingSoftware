using Billing.Application.DTOs.Common;

namespace Billing.Application.DTOs.StockLedger
{
    public class StockLedgerDto
    {
        public int Id { get; set; }
        public int ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public string? Sku { get; set; }
        public string TransactionType { get; set; } = string.Empty;

        /// <summary>Units moved (always positive). See <see cref="QuantityChange"/> for the direction.</summary>
        public int Quantity { get; set; }

        /// <summary>Signed effect on stock: CurrentStock minus PreviousStock.</summary>
        public int QuantityChange { get; set; }
        public int PreviousStock { get; set; }
        public int CurrentStock { get; set; }
        public string? ReferenceNo { get; set; }
        public string? Remarks { get; set; }
        public string? CreatedBy { get; set; }
        public DateTime CreatedOn { get; set; }
    }

    public class StockLedgerSearchRequest : PaginationRequest
    {
        /// <summary>Matches product name, SKU or reference number.</summary>
        public string? Search { get; set; }
        public int? ProductId { get; set; }
        public string? TransactionType { get; set; }
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
    }
}