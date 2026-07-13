using Billing.API.Models;
using Billing.Domain.Entities;
using Billing.Persistence.Context;
using Microsoft.EntityFrameworkCore;

namespace Billing.API.Services;

public class SupplierService
{
    private readonly BillingDbContext _context;

    public SupplierService(BillingDbContext context)
    {
        _context = context;
    }

    public async Task<int> CreateAsync(SupplierDto dto)
    {
        if (await _context.Suppliers.AnyAsync(x =>
            x.Mobile == dto.Mobile &&
            !x.IsDeleted))
        {
            throw new Exception("Mobile number already exists.");
        }

        var supplier = new Supplier
        {
            Name = dto.Name,
            CompanyName = dto.CompanyName,
            Mobile = dto.Mobile,
            Email = dto.Email,
            Address = dto.Address
        };

        _context.Suppliers.Add(supplier);

        await _context.SaveChangesAsync();

        return supplier.Id;
    }

    public async Task<List<SupplierDto>> GetAllAsync()
    {
        return await _context.Suppliers
            .Where(x => !x.IsDeleted)
            .OrderBy(x => x.Name)
            .Select(x => new SupplierDto
            {
                Id = x.Id,
                Name = x.Name,
                CompanyName = x.CompanyName,
                Mobile = x.Mobile,
                Email = x.Email,
                Address = x.Address
            })
            .ToListAsync();
    }

    public async Task<SupplierDto?> GetByIdAsync(int id)
    {
        return await _context.Suppliers
            .Where(x => x.Id == id && !x.IsDeleted)
            .Select(x => new SupplierDto
            {
                Id = x.Id,
                Name = x.Name,
                CompanyName = x.CompanyName,
                Mobile = x.Mobile,
                Email = x.Email,
                Address = x.Address
            })
            .FirstOrDefaultAsync();
    }

    public async Task UpdateAsync(int id, SupplierDto dto)
    {
        if (await _context.Suppliers.AnyAsync(x =>
            x.Id != id &&
            x.Mobile == dto.Mobile &&
            !x.IsDeleted))
        {
            throw new Exception("Mobile number already exists.");
        }

        var supplier = await _context.Suppliers.FindAsync(id);

        if (supplier == null || supplier.IsDeleted)
            throw new Exception("Supplier not found.");

        supplier.Name = dto.Name;
        supplier.CompanyName = dto.CompanyName;
        supplier.Mobile = dto.Mobile;
        supplier.Email = dto.Email;
        supplier.Address = dto.Address;

        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(int id)
    {
        var supplier = await _context.Suppliers.FindAsync(id);

        if (supplier == null || supplier.IsDeleted)
            throw new Exception("Supplier not found.");

        supplier.IsDeleted = true;

        await _context.SaveChangesAsync();
    }

    public async Task<PagedResult<SupplierDto>> SearchAsync(SupplierSearchRequest request)
    {
        var query = _context.Suppliers.Where(x => !x.IsDeleted);

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            query = query.Where(x =>
                x.Name.Contains(request.Search) ||
                x.CompanyName.Contains(request.Search) ||
                x.Mobile.Contains(request.Search));
        }

        int totalRecords = await query.CountAsync();

        var data = await query
            .OrderBy(x => x.Name)
            .Skip((request.PageNumber - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(x => new SupplierDto
            {
                Id = x.Id,
                Name = x.Name,
                CompanyName = x.CompanyName,
                Mobile = x.Mobile,
                Email = x.Email,
                Address = x.Address
            })
            .ToListAsync();

        return new PagedResult<SupplierDto>
        {
            TotalRecords = totalRecords,
            TotalPages = (int)Math.Ceiling((double)totalRecords / request.PageSize),
            PageNumber = request.PageNumber,
            PageSize = request.PageSize,
            Data = data
        };
    }
}