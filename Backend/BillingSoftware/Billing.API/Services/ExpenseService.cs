using Billing.Application.DTOs.Expense;
using Billing.Application.Interfaces;
using Billing.Domain.Entities;
using Billing.Persistence.Context;
using Microsoft.EntityFrameworkCore;

namespace Billing.API.Services;

public class ExpenseService : IExpenseService
{
    private readonly BillingDbContext _context;

    public ExpenseService(BillingDbContext context)
    {
        _context = context;
    }

    public async Task CreateAsync(CreateExpenseDto dto)
    {
        var expense = new Expense
        {
            Title = dto.Title,
            Amount = dto.Amount,
            ExpenseDate = dto.ExpenseDate,
            Category = dto.Category,
            Remarks = dto.Remarks,
            CreatedOn = DateTime.UtcNow
        };

        _context.Expenses.Add(expense);

        await _context.SaveChangesAsync();
    }

    public async Task<List<ExpenseDto>> GetAllAsync()
    {
        return await _context.Expenses
            .Where(x => !x.IsDeleted)
            .OrderByDescending(x => x.ExpenseDate)
            .Select(x => new ExpenseDto
            {
                Id = x.Id,
                Title = x.Title,
                Amount = x.Amount,
                ExpenseDate = x.ExpenseDate,
                Category = x.Category,
                Remarks = x.Remarks
            })
            .ToListAsync();
    }

    public async Task<ExpenseDto?> GetByIdAsync(int id)
    {
        return await _context.Expenses
            .Where(x => x.Id == id && !x.IsDeleted)
            .Select(x => new ExpenseDto
            {
                Id = x.Id,
                Title = x.Title,
                Amount = x.Amount,
                ExpenseDate = x.ExpenseDate,
                Category = x.Category,
                Remarks = x.Remarks
            })
            .FirstOrDefaultAsync();
    }

    public async Task UpdateAsync(int id, CreateExpenseDto dto)
    {
        var expense = await _context.Expenses
            .FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted);

        if (expense == null)
            throw new Exception("Expense not found.");

        expense.Title = dto.Title;
        expense.Amount = dto.Amount;
        expense.ExpenseDate = dto.ExpenseDate;
        expense.Category = dto.Category;
        expense.Remarks = dto.Remarks;
        expense.modifieson = DateTime.UtcNow;

        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(int id)
    {
        var expense = await _context.Expenses
            .FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted);

        if (expense == null)
            throw new Exception("Expense not found.");

        expense.IsDeleted = true;
        expense.modifieson = DateTime.UtcNow;

        await _context.SaveChangesAsync();
    }
}