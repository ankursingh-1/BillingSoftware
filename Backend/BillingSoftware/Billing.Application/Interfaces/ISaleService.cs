using Billing.Application.DTOs.Sales;

namespace Billing.Application.Interfaces
{
    public interface ISaleService
    {
        Task<SaleResponse> CreateSaleAsync(CreateSaleRequest request);
    }
}