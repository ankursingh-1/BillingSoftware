using Billing.Application.DTOs.StockLedger;

namespace Billing.Application.Interfaces
{
    public interface IStockLedgerService
    {
        Task<List<StockLedgerDto>> GetAllAsync();
        Task<List<StockLedgerDto>> GetByProductAsync(int productId);
    }
}