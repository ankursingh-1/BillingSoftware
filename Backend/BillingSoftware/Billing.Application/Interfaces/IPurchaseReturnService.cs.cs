using Billing.Application.DTOs.PurchaseReturn;

namespace Billing.Application.Interfaces
{
    public interface IPurchaseReturnService
    {
        Task CreateAsync(CreatePurchaseReturnDto dto);
        Task<List<PurchaseReturnDto>> GetAllAsync();
    }
}