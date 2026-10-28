using Billing.Application.DTOs.Common;
using Billing.Application.DTOs.Expense;
using Billing.Application.Interfaces;
using Billing.Domain.Entities;
using Billing.Persistence.Context;
using Billing.Shared.Exceptions;
using Microsoft.EntityFrameworkCore;

namespace Billing.API.Services;

public class ExpenseService : IExpenseService
{
    private const int MaxPageSize = 100;
    private readonly BillingDbContext _context;

    public ExpenseService(BillingDbContext context)
    {
        _context = context;
    }

    public async Task CreateAsync(CreateExpenseDto dto)
    {
        Validate(dto);

        _context.Expenses.Add(new Expense
        {
            Title = dto.Title.Trim(),
            Amount = decimal.Round(dto.Amount, 2, MidpointRounding.AwayFromZero),
            ExpenseDate = dto.ExpenseDate.Date,
            Category = Normalize(dto.Category),
            Remarks = Normalize(dto.Remarks),
            CreatedOn = DateTime.UtcNow
        });

        await _context.SaveChangesAsync();
    }

    public async Task<List<ExpenseDto>> GetAllAsync()
    {
        return await Query()
            .OrderByDescending(x => x.ExpenseDate)
            .ThenByDescending(x => x.Id)
            .Select(ToDto)
            .ToListAsync();
    }

    public async Task<PagedResult<ExpenseDto>> SearchAsync(ExpenseSearchRequest request)
    {
        var pageSize = Math.Clamp(request.PageSize, 1, MaxPageSize);
        var pageNumber = Math.Max(request.PageNumber, 1);
        var query = Query();

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var search = request.Search.Trim();
            query = query.Where(x =>
                x.Title.Contains(search) ||
                (x.Category != null && x.Category.Contains(search)) ||
                (x.Remarks != null && x.Remarks.Contains(search)));
        }

        if (request.FromDate.HasValue)
            query = query.Where(x => x.ExpenseDate >= request.FromDate.Value.Date);

        if (request.ToDate.HasValue)
            query = query.Where(x => x.ExpenseDate <= request.ToDate.Value.Date);

        if (!string.IsNullOrWhiteSpace(request.Category))
        {
            var category = request.Category.Trim();
            query = query.Where(x => x.Category == category);
        }

        var total = await query.CountAsync();
        var data = await query
            .OrderByDescending(x => x.ExpenseDate)
            .ThenByDescending(x => x.Id)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .Select(ToDto)
            .ToListAsync();

        return new PagedResult<ExpenseDto>
        {
            TotalRecords = total,
            TotalPages = (int)Math.Ceiling(total / (double)pageSize),
            PageNumber = pageNumber,
            PageSize = pageSize,
            Items = data
        };
    }

    public async Task<ExpenseDto?> GetByIdAsync(int id)
    {
        return await Query()
            .Where(x => x.Id == id)
            .Select(ToDto)
            .FirstOrDefaultAsync();
    }

    public async Task UpdateAsync(int id, CreateExpenseDto dto)
    {
        Validate(dto);

        var expense = await _context.Expenses
            .FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted)
            ?? throw BusinessException.NotFound("Expense not found.");

        expense.Title = dto.Title.Trim();
        expense.Amount = decimal.Round(dto.Amount, 2, MidpointRounding.AwayFromZero);
        expense.ExpenseDate = dto.ExpenseDate.Date;
        expense.Category = Normalize(dto.Category);
        expense.Remarks = Normalize(dto.Remarks);
        expense.modifieson = DateTime.UtcNow;

        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(int id)
    {
        var expense = await _context.Expenses
            .FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted)
            ?? throw BusinessException.NotFound("Expense not found.");

        expense.IsDeleted = true;
        expense.modifieson = DateTime.UtcNow;

        await _context.SaveChangesAsync();
    }

    private IQueryable<Expense> Query() =>
        _context.Expenses.AsNoTracking().Where(x => !x.IsDeleted);

    private static readonly System.Linq.Expressions.Expression<Func<Expense, ExpenseDto>> ToDto =
        x => new ExpenseDto
        {
            Id = x.Id,
            Title = x.Title,
            Amount = x.Amount,
            ExpenseDate = x.ExpenseDate,
            Category = x.Category,
            Remarks = x.Remarks
        };

    private static void Validate(CreateExpenseDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Title))
            throw new BusinessException("Expense title is required.");

        if (dto.Amount <= 0)
            throw new BusinessException("Amount must be greater than 0.");

        if (dto.ExpenseDate == default)
            throw new BusinessException("Expense date is required.");
    }

    private static string? Normalize(string? value)
    {
        var trimmed = value?.Trim();
        return string.IsNullOrEmpty(trimmed) ? null : trimmed;
    }
}
