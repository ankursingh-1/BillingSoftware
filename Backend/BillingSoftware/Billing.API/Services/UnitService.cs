using Billing.Application.DTOs.Unit;
using Billing.Application.Interfaces;
using Billing.Domain.Entities;
using Billing.Persistence.Context;
using Microsoft.EntityFrameworkCore;

namespace Billing.API.Services;

public class UnitService : IUnitService
{
    private readonly BillingDbContext _context;

    public UnitService(BillingDbContext context)
    {
        _context = context;
    }

    public async Task<List<UnitDto>> GetAllAsync()
    {
        return await _context.Units
            .Where(x => !x.IsDeleted)
            .OrderBy(x => x.Name)
            .Select(x => new UnitDto
            {
                Id = x.Id,
                Name = x.Name,
                ShortName = x.ShortName,
                Description = x.Description,
                IsActive = x.IsActive
            })
            .ToListAsync();
    }

    public async Task<UnitDto?> GetByIdAsync(int id)
    {
        return await _context.Units
            .Where(x => x.Id == id && !x.IsDeleted)
            .Select(x => new UnitDto
            {
                Id = x.Id,
                Name = x.Name,
                ShortName = x.ShortName,
                Description = x.Description,
                IsActive = x.IsActive
            })
            .FirstOrDefaultAsync();
    }

    public async Task<UnitDto> CreateAsync(SaveUnitRequest request)
    {
        if (await _context.Units.AnyAsync(x => x.Name == request.Name && !x.IsDeleted))
        {
            throw new Exception("Unit already exists.");
        }

        var unit = new Unit
        {
            Name = request.Name,
            ShortName = request.ShortName,
            Description = request.Description,
            IsActive = request.IsActive
        };

        _context.Units.Add(unit);
        await _context.SaveChangesAsync();

        return new UnitDto
        {
            Id = unit.Id,
            Name = unit.Name,
            ShortName = unit.ShortName,
            Description = unit.Description,
            IsActive = unit.IsActive
        };
    }

    public async Task<UnitDto?> UpdateAsync(int id, SaveUnitRequest request)
    {
        var unit = await _context.Units
            .FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted);

        if (unit == null)
            return null;

        if (await _context.Units.AnyAsync(x =>
                x.Id != id &&
                x.Name == request.Name &&
                !x.IsDeleted))
        {
            throw new Exception("Unit already exists.");
        }

        unit.Name = request.Name;
        unit.ShortName = request.ShortName;
        unit.Description = request.Description;
        unit.IsActive = request.IsActive;

        await _context.SaveChangesAsync();

        return new UnitDto
        {
            Id = unit.Id,
            Name = unit.Name,
            ShortName = unit.ShortName,
            Description = unit.Description,
            IsActive = unit.IsActive
        };
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var unit = await _context.Units
            .FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted);

        if (unit == null)
            return false;

        unit.IsDeleted = true;

        await _context.SaveChangesAsync();

        return true;
    }
}