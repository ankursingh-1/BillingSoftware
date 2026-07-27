using Billing.Application.DTOs.Tax;

namespace Billing.Application.Interfaces
{
    public interface ITaxService
    {
        Task<List<TaxDto>> GetAllAsync();
        Task<TaxDto?> GetByIdAsync(int id);
        Task<TaxDto> CreateAsync(SaveTaxRequest request);
        Task<TaxDto?> UpdateAsync(int id, SaveTaxRequest request);
        Task<bool> DeleteAsync(int id);
    }
}