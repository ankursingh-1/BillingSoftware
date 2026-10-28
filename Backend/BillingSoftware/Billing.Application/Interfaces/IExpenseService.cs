using Billing.Application.DTOs.Common;
using Billing.Application.DTOs.Expense;

namespace Billing.Application.Interfaces;

public interface IExpenseService
{
    Task CreateAsync(CreateExpenseDto dto);
    Task<List<ExpenseDto>> GetAllAsync();
    Task<PagedResult<ExpenseDto>> SearchAsync(ExpenseSearchRequest request);
    Task<ExpenseDto?> GetByIdAsync(int id);
    Task UpdateAsync(int id, CreateExpenseDto dto);
    Task DeleteAsync(int id);
}
