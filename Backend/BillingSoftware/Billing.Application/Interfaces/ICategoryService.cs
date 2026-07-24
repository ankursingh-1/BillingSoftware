using Billing.Application.DTOs.Categories;
using Billing.Application.DTOs.Category;

namespace Billing.Application.Interfaces;

public interface ICategoryService
{
    Task<List<CategoryDto>> GetAllAsync();
    Task<CategoryDto?> GetByIdAsync(int id);
    Task<CategoryDto> CreateAsync(SaveCategoryRequest request);
    Task<CategoryDto?> UpdateAsync(int id, SaveCategoryRequest request);
    Task<bool> DeleteAsync(int id);
}