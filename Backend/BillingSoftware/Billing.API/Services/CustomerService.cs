using Billing.API.Models;
using Billing.Domain.Entities;
using Billing.Persistence.Context;
using Microsoft.EntityFrameworkCore;

namespace Billing.API.Services;

public class CustomerService
{
    private readonly BillingDbContext _context;

    public CustomerService(BillingDbContext context)
    {
        _context = context;
    }

    public async Task<int> CreateAsync(CustomerDto dto)
    {
        if (await _context.Customers.AnyAsync(x =>
            x.Mobile == dto.Mobile &&
            !x.IsDeleted))
        {
            throw new Exception("Mobile number already exists.");
        }

        var customer = new Customer
        {
            Name = dto.Name,
            Mobile = dto.Mobile,
            Email = dto.Email,
            Address = dto.Address
        };

        _context.Customers.Add(customer);

        await _context.SaveChangesAsync();

        return customer.Id;
    }

    public async Task<List<CustomerDto>> GetAllAsync()
    {
        return await _context.Customers
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
        if (await _context.Customers.AnyAsync(x =>
            x.Id != id &&
            x.Mobile == dto.Mobile &&
            !x.IsDeleted))
        {
            throw new Exception("Mobile number already exists.");
        }

        var customer = await _context.Customers.FindAsync(id);

        if (customer == null || customer.IsDeleted)
            throw new Exception("Customer not found.");

        customer.Name = dto.Name;
        customer.Mobile = dto.Mobile;
        customer.Email = dto.Email;
        customer.Address = dto.Address;

        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(int id)
    {
        var customer = await _context.Customers.FindAsync(id);

        if (customer == null || customer.IsDeleted)
            throw new Exception("Customer not found.");

        customer.IsDeleted = true;

        await _context.SaveChangesAsync();
    }

    public async Task<PagedResult<CustomerDto>> SearchAsync(CustomerSearchRequest request)
    {
        var query = _context.Customers.Where(x => !x.IsDeleted);

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            query = query.Where(x =>
                x.Name.Contains(request.Search) ||
                x.Mobile.Contains(request.Search));
        }

        int totalRecords = await query.CountAsync();

        var data = await query
            .OrderBy(x => x.Name)
            .Skip((request.PageNumber - 1) * request.PageSize)
            .Take(request.PageSize)
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
            TotalPages = (int)Math.Ceiling((double)totalRecords / request.PageSize),
            PageNumber = request.PageNumber,
            PageSize = request.PageSize,
            Data = data
        };
    }
}