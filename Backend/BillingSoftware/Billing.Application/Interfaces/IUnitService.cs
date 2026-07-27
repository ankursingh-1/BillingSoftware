using Billing.Application.DTOs;
using Billing.Application.DTOs.Unit;

namespace Billing.Application.Interfaces;

public interface IUnitService
{
    Task<List<UnitDto>> GetAllAsync();
    Task<UnitDto?> GetByIdAsync(int id);
    Task<UnitDto> CreateAsync(SaveUnitRequest request);
    Task<UnitDto?> UpdateAsync(int id, SaveUnitRequest request);
    Task<bool> DeleteAsync(int id);
}