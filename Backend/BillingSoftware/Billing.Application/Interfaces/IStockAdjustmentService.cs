using Billing.Application.DTOs.StockAdjustment;

namespace Billing.Application.Interfaces
{
    public interface IStockAdjustmentService
    {
        Task AdjustStockAsync(CreateStockAdjustmentDto dto);
        Task<List<StockAdjustmentDto>> GetHistoryAsync();
    }
}