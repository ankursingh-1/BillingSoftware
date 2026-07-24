using Billing.Application.DTOs.Brand;

namespace Billing.Application.Interfaces;

public interface IBrandService
{
    Task<List<BrandDto>> GetAllAsync();
    Task<BrandDto?> GetByIdAsync(int id);
    Task<BrandDto> CreateAsync(SaveBrandRequest request);
    Task<BrandDto?> UpdateAsync(int id, SaveBrandRequest request);
    Task<bool> DeleteAsync(int id);
}