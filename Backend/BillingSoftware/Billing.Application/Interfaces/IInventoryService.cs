using Billing.Domain.Enums;

namespace Billing.Application.Interfaces
{
    public interface IInventoryService
    {
        Task AdjustStockAsync(
            int productId,
            int quantity,
            StockTransactionType transactionType,
            string? referenceNo = null,
            string? remarks = null);
    }
}