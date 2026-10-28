using Billing.API.Models;
using Billing.Domain.Entities;
using Billing.Persistence.Context;
using Billing.Shared.Exceptions;
using Microsoft.EntityFrameworkCore;

namespace Billing.API.Services;

public class CustomerService
{
    private const int MaxPageSize = 100;

    private readonly BillingDbContext _context;

    public CustomerService(BillingDbContext context)
    {
        _context = context;
    }

    public async Task<int> CreateAsync(CustomerDto dto)
    {
        var mobile = dto.Mobile.Trim();

        await EnsureMobileIsFreeAsync(mobile, excludeId: null);

        var customer = new Customer
        {
            Name = dto.Name.Trim(),
            Mobile = mobile,
            Email = NormalizeOptional(dto.Email),
            Address = NormalizeOptional(dto.Address)
        };

        _context.Customers.Add(customer);

        await _context.SaveChangesAsync();

        return customer.Id;
    }

    public async Task<List<CustomerDto>> GetAllAsync()
    {
        return await _context.Customers
            .AsNoTracking()
            .Where(x => !x.IsDeleted)
            .OrderBy(x => x.Name)
            .Select(x => new CustomerDto
            {
                Id = x.Id,
                Name = x.Name,
                Mobile = x.Mobile,
                Email = x.Email,
                Address = x.Address
            })
            .ToListAsync();
    }

    public async Task<CustomerDto?> GetByIdAsync(int id)
    {
        return await _context.Customers
            .AsNoTracking()
            .Where(x => x.Id == id && !x.IsDeleted)
            .Select(x => new CustomerDto
            {
                Id = x.Id,
                Name = x.Name,
                Mobile = x.Mobile,
                Email = x.Email,
                Address = x.Address
            })
            .FirstOrDefaultAsync();
    }

    public async Task UpdateAsync(int id, CustomerDto dto)
    {
        var customer = await _context.Customers.FindAsync(id);

        if (customer == null || customer.IsDeleted)
            throw BusinessException.NotFound("Customer not found.");

        var mobile = dto.Mobile.Trim();

        await EnsureMobileIsFreeAsync(mobile, excludeId: id);

        customer.Name = dto.Name.Trim();
        customer.Mobile = mobile;
        customer.Email = NormalizeOptional(dto.Email);
        customer.Address = NormalizeOptional(dto.Address);

        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(int id)
    {
        var customer = await _context.Customers.FindAsync(id);

        if (customer == null || customer.IsDeleted)
            throw BusinessException.NotFound("Customer not found.");

        // Soft delete: existing sales keep pointing at this customer.
        customer.IsDeleted = true;

        await _context.SaveChangesAsync();
    }

    public async Task<PagedResult<CustomerDto>> SearchAsync(CustomerSearchRequest request)
    {
        var pageNumber = Math.Max(request.PageNumber, 1);
        var pageSize = Math.Clamp(request.PageSize, 1, MaxPageSize);

        var query = _context.Customers
            .AsNoTracking()
            .Where(x => !x.IsDeleted);

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var search = request.Search.Trim();

            query = query.Where(x =>
                x.Name.Contains(search) ||
                x.Mobile.Contains(search));
        }

        int totalRecords = await query.CountAsync();

        var data = await query
            .OrderBy(x => x.Name)
            .ThenBy(x => x.Id)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new CustomerDto
            {
                Id = x.Id,
                Name = x.Name,
                Mobile = x.Mobile,
                Email = x.Email,
                Address = x.Address
            })
            .ToListAsync();

        return new PagedResult<CustomerDto>
        {
            TotalRecords = totalRecords,
            TotalPages = (int)Math.Ceiling((double)totalRecords / pageSize),
            PageNumber = pageNumber,
            PageSize = pageSize,
            Data = data
        };
    }

    private async Task EnsureMobileIsFreeAsync(string mobile, int? excludeId)
    {
        if (await _context.Customers.AnyAsync(x =>
            x.Id != excludeId &&
            x.Mobile == mobile &&
            !x.IsDeleted))
        {
            throw BusinessException.Conflict("Mobile number already exists.");
        }
    }

    private static string? NormalizeOptional(string? value)
    {
        var trimmed = value?.Trim();

        return string.IsNullOrEmpty(trimmed) ? null : trimmed;
    }
}
