using Billing.Application.DTOs.Brand;
using Billing.Application.Interfaces;
using Billing.Domain.Entities;
using Billing.Persistence.Context;
using Microsoft.EntityFrameworkCore;

namespace Billing.API.Services;

public class BrandService : IBrandService
{
    private readonly BillingDbContext _context;

    public BrandService(BillingDbContext context)
    {
        _context = context;
    }

    public async Task<List<BrandDto>> GetAllAsync()
    {
        return await _context.Brands
            .Where(x => !x.IsDeleted)
            .OrderBy(x => x.Name)
            .Select(x => new BrandDto
            {
                Id = x.Id,
                Name = x.Name,
                Description = x.Description,
                IsActive = x.IsActive
            })
            .ToListAsync();
    }

    public async Task<BrandDto?> GetByIdAsync(int id)
    {
        return await _context.Brands
            .Where(x => x.Id == id && !x.IsDeleted)
            .Select(x => new BrandDto
            {
                Id = x.Id,
                Name = x.Name,
                Description = x.Description,
                IsActive = x.IsActive
            })
            .FirstOrDefaultAsync();
    }

    public async Task<BrandDto> CreateAsync(SaveBrandRequest request)
    {
        if (await _context.Brands.AnyAsync(x => x.Name == request.Name && !x.IsDeleted))
        {
            throw new Exception("Brand already exists.");
        }

        var brand = new Brand
        {
            Name = request.Name,
            Description = request.Description,
            IsActive = request.IsActive
        };

        _context.Brands.Add(brand);
        await _context.SaveChangesAsync();

        return new BrandDto
        {
            Id = brand.Id,
            Name = brand.Name,
            Description = brand.Description,
            IsActive = brand.IsActive
        };
    }

    public async Task<BrandDto?> UpdateAsync(int id, SaveBrandRequest request)
    {
        var brand = await _context.Brands
            .FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted);

        if (brand == null)
            return null;

        if (await _context.Brands.AnyAsync(x =>
                x.Id != id &&
                x.Name == request.Name &&
                !x.IsDeleted))
        {
            throw new Exception("Brand already exists.");
        }

        brand.Name = request.Name;
        brand.Description = request.Description;
        brand.IsActive = request.IsActive;

        await _context.SaveChangesAsync();

        return new BrandDto
        {
            Id = brand.Id,
            Name = brand.Name,
            Description = brand.Description,
            IsActive = brand.IsActive
        };
    }
    public async Task<bool> DeleteAsync(int id)
    {
        var brand = await _context.Brands
            .FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted);
        if (brand == null)
            return false;
        brand.IsDeleted = true;
        await _context.SaveChangesAsync();

        return true;
    }
}