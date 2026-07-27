using Billing.Domain.Common;
using Billing.Domain.Enums;

namespace Billing.Domain.Entities
{
    public class StockLedger : SoftDeleteEntity
    {
        public int ProductId { get; set; }
        public Product Product { get; set; } = null!;
        public StockTransactionType TransactionType { get; set; }
        public int Quantity { get; set; }
        public int PreviousStock { get; set; }
        public int CurrentStock { get; set; }
        public string? ReferenceNo { get; set; }
        public string? Remarks { get; set; }
    }
}