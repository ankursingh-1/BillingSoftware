using Billing.API.Models;
using Billing.Domain.Entities;
using Billing.Persistence.Context;
using Billing.Shared.Exceptions;
using Microsoft.EntityFrameworkCore;

namespace Billing.API.Services;

public class SupplierService
{
    private const int MaxPageSize = 100;

    private readonly BillingDbContext _context;

    public SupplierService(BillingDbContext context)
    {
        _context = context;
    }

    public async Task<int> CreateAsync(SupplierDto dto)
    {
        var mobile = dto.Mobile.Trim();

        await EnsureMobileIsFreeAsync(mobile, excludeId: null);
        var tax = NormalizeTax(dto);

        var supplier = new Supplier
        {
            Name = dto.Name.Trim(),
            CompanyName = dto.CompanyName.Trim(),
            Mobile = mobile,
            Email = NormalizeOptional(dto.Email),
            Address = NormalizeOptional(dto.Address),
            State = tax.State,
            StateCode = tax.StateCode,
            Gstin = tax.Gstin
        };

        _context.Suppliers.Add(supplier);

        await _context.SaveChangesAsync();

        return supplier.Id;
    }

    public async Task<List<SupplierDto>> GetAllAsync()
    {
        return await _context.Suppliers
            .AsNoTracking()
            .Where(x => !x.IsDeleted)
            .OrderBy(x => x.Name)
            .Select(x => new SupplierDto
            {
                Id = x.Id,
                Name = x.Name,
                CompanyName = x.CompanyName,
                Mobile = x.Mobile,
                Email = x.Email,
                Address = x.Address,
                State = x.State,
                StateCode = x.StateCode,
                Gstin = x.Gstin
            })
            .ToListAsync();
    }

    public async Task<SupplierDto?> GetByIdAsync(int id)
    {
        return await _context.Suppliers
            .AsNoTracking()
            .Where(x => x.Id == id && !x.IsDeleted)
            .Select(x => new SupplierDto
            {
                Id = x.Id,
                Name = x.Name,
                CompanyName = x.CompanyName,
                Mobile = x.Mobile,
                Email = x.Email,
                Address = x.Address,
                State = x.State,
                StateCode = x.StateCode,
                Gstin = x.Gstin
            })
            .FirstOrDefaultAsync();
    }

    public async Task UpdateAsync(int id, SupplierDto dto)
    {
        var supplier = await _context.Suppliers.FindAsync(id);

        if (supplier == null || supplier.IsDeleted)
            throw BusinessException.NotFound("Supplier not found.");

        var mobile = dto.Mobile.Trim();

        await EnsureMobileIsFreeAsync(mobile, excludeId: id);
        var tax = NormalizeTax(dto);

        supplier.Name = dto.Name.Trim();
        supplier.CompanyName = dto.CompanyName.Trim();
        supplier.Mobile = mobile;
        supplier.Email = NormalizeOptional(dto.Email);
        supplier.Address = NormalizeOptional(dto.Address);
        supplier.State = tax.State;
        supplier.StateCode = tax.StateCode;
        supplier.Gstin = tax.Gstin;

        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(int id)
    {
        var supplier = await _context.Suppliers.FindAsync(id);

        if (supplier == null || supplier.IsDeleted)
            throw BusinessException.NotFound("Supplier not found.");

        // Soft delete: existing purchases keep pointing at this supplier.
        supplier.IsDeleted = true;

        await _context.SaveChangesAsync();
    }

    public async Task<PagedResult<SupplierDto>> SearchAsync(SupplierSearchRequest request)
    {
        var pageNumber = Math.Max(request.PageNumber, 1);
        var pageSize = Math.Clamp(request.PageSize, 1, MaxPageSize);

        var query = _context.Suppliers
            .AsNoTracking()
            .Where(x => !x.IsDeleted);

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var search = request.Search.Trim();

            query = query.Where(x =>
                x.Name.Contains(search) ||
                x.CompanyName.Contains(search) ||
                x.Mobile.Contains(search));
        }

        int totalRecords = await query.CountAsync();

        var data = await query
            .OrderBy(x => x.Name)
            .ThenBy(x => x.Id)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new SupplierDto
            {
                Id = x.Id,
                Name = x.Name,
                CompanyName = x.CompanyName,
                Mobile = x.Mobile,
                Email = x.Email,
                Address = x.Address,
                State = x.State,
                StateCode = x.StateCode,
                Gstin = x.Gstin
            })
            .ToListAsync();

        return new PagedResult<SupplierDto>
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
        if (await _context.Suppliers.AnyAsync(x =>
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

    private static Billing.Application.Accounting.PartyTaxInfo NormalizeTax(SupplierDto dto)
    {
        try
        {
            return Billing.Application.Accounting.PartyTaxDetails.Normalize(dto.State, dto.StateCode, dto.Gstin);
        }
        catch (Billing.Application.Accounting.PartyTaxException ex)
        {
            throw new BusinessException(ex.Message);
        }
    }
}
