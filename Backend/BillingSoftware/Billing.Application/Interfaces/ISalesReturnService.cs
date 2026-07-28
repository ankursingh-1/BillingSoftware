using Billing.Application.DTOs.SalesReturn;

namespace Billing.Application.Interfaces
{
    public interface ISalesReturnService
    {
        Task CreateAsync(CreateSalesReturnDto dto);
        Task<List<SalesReturnDto>> GetAllAsync();
    }
}