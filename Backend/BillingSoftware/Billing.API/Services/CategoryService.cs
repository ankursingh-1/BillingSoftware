using Billing.Application.DTOs.Categories;
using Billing.Application.DTOs.Category;
using Billing.Application.Interfaces;
using Billing.Domain.Entities;
using Billing.Persistence.Context;
using Microsoft.EntityFrameworkCore;

namespace Billing.API.Services;

public class CategoryService : ICategoryService
{
    private readonly BillingDbContext _context;

    public CategoryService(BillingDbContext context)
    {
        _context = context;
    }

    public async Task<List<CategoryDto>> GetAllAsync()
    {
        return await _context.Categories
            .Where(x => !x.IsDeleted)
            .OrderBy(x => x.Name)
            .Select(x => new CategoryDto
            {
                Id = x.Id,
                Name = x.Name,
                Description = x.Description,
                IsActive = x.IsActive
            })
            .ToListAsync();
    }

    public async Task<CategoryDto?> GetByIdAsync(int id)
    {
        return await _context.Categories
            .Where(x => x.Id == id && !x.IsDeleted)
            .Select(x => new CategoryDto
            {
                Id = x.Id,
                Name = x.Name,
                Description = x.Description,
                IsActive = x.IsActive
            })
            .FirstOrDefaultAsync();
    }

    public async Task<CategoryDto> CreateAsync(SaveCategoryRequest request)
    {
        if (await _context.Categories.AnyAsync(x =>
            x.Name == request.Name &&
            !x.IsDeleted))
        {
            throw new Exception("Category already exists.");
        }

        var category = new Category
        {
            Name = request.Name,
            Description = request.Description,
            IsActive = request.IsActive
        };

        _context.Categories.Add(category);

        await _context.SaveChangesAsync();

        return await GetByIdAsync(category.Id)
               ?? throw new Exception("Category creation failed.");
    }

    public async Task<CategoryDto?> UpdateAsync(int id, SaveCategoryRequest request)
    {
        if (await _context.Categories.AnyAsync(x =>
            x.Id != id &&
            x.Name == request.Name &&
            !x.IsDeleted))
        {
            throw new Exception("Category already exists.");
        }

        var category = await _context.Categories.FirstOrDefaultAsync(x => x.Id == id);

        if (category == null || category.IsDeleted)
            throw new Exception("Category not found.");

        category.Name = request.Name;
        category.Description = request.Description;
        category.IsActive = request.IsActive;

        await _context.SaveChangesAsync();

        return await GetByIdAsync(id);
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var category = await _context.Categories.FirstOrDefaultAsync(x => x.Id == id);

        if (category == null || category.IsDeleted)
            throw new Exception("Category not found.");

        category.IsDeleted = true;

        await _context.SaveChangesAsync();

        return true;
    }
}