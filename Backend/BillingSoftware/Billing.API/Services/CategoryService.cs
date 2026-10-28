using Billing.Application.DTOs.Categories;
using Billing.Application.DTOs.Category;
using Billing.Application.Interfaces;
using Billing.Domain.Entities;
using Billing.Persistence.Context;
using Billing.Shared.Exceptions;
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
        var name = request.Name.Trim();

        if (name.Length == 0)
            throw new BusinessException("Category name is required.");

        await EnsureNameIsFreeAsync(name, null);

        var category = new Category
        {
            Name = name,
            Description = NormalizeDescription(request.Description),
            IsActive = request.IsActive
        };

        _context.Categories.Add(category);

        await _context.SaveChangesAsync();

        return await GetByIdAsync(category.Id)
               ?? throw new Exception("Category creation failed.");
    }

    public async Task<CategoryDto?> UpdateAsync(int id, SaveCategoryRequest request)
    {
        var name = request.Name.Trim();

        if (name.Length == 0)
            throw new BusinessException("Category name is required.");

        var category = await _context.Categories.FirstOrDefaultAsync(x => x.Id == id);

        if (category == null || category.IsDeleted)
            throw BusinessException.NotFound("Category not found.");

        await EnsureNameIsFreeAsync(name, id);

        category.Name = name;
        category.Description = NormalizeDescription(request.Description);
        category.IsActive = request.IsActive;

        await _context.SaveChangesAsync();

        return await GetByIdAsync(id);
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var category = await _context.Categories.FirstOrDefaultAsync(x => x.Id == id);

        if (category == null || category.IsDeleted)
            throw BusinessException.NotFound("Category not found.");

        int productsUsingCategory = await _context.Products
            .CountAsync(x => x.CategoryId == id && !x.IsDeleted);

        if (productsUsingCategory > 0)
        {
            throw BusinessException.Conflict(
                $"Category is used by {productsUsingCategory} product(s). " +
                "Reassign or delete those products first, or mark the category inactive.");
        }

        category.IsDeleted = true;

        await _context.SaveChangesAsync();

        return true;
    }

    // Categories.Name has a unique database index that also covers
    // soft-deleted rows, so deleted names must be checked too.
    private async Task EnsureNameIsFreeAsync(string name, int? excludeId)
    {
        var existing = await _context.Categories
            .Where(x => x.Name == name && (excludeId == null || x.Id != excludeId))
            .Select(x => new { x.IsDeleted })
            .FirstOrDefaultAsync();

        if (existing == null)
            return;

        throw BusinessException.Conflict(
            existing.IsDeleted
                ? "A deleted category with this name already exists. Choose a different name."
                : "Category already exists.");
    }

    private static string? NormalizeDescription(string? description) =>
        string.IsNullOrWhiteSpace(description) ? null : description.Trim();
}