using Billing.API.Models;
using Billing.Domain.Entities;
using Billing.Persistence.Context;
using Microsoft.EntityFrameworkCore;

namespace Billing.API.Services;

public class ProductService
{
    private readonly BillingDbContext _context;

    public ProductService(BillingDbContext context)
    {
        _context = context;
    }

    public async Task<int> CreateAsync(ProductDto dto)
    {
        if (await _context.Products.AnyAsync(x => x.SKU == dto.SKU && !x.IsDeleted))
            throw new Exception("SKU already exists.");

        var product = new Product
        {
            Name = dto.Name,
            SKU = dto.SKU,
            PurchasePrice = dto.PurchasePrice,
            SellingPrice = dto.SellingPrice,
            Stock = dto.Stock
        };

        _context.Products.Add(product);

        await _context.SaveChangesAsync();

        return product.Id;
    }

    public async Task<List<ProductDto>> GetAllAsync()
    {
        return await _context.Products
            .Where(x => !x.IsDeleted)
            .OrderBy(x => x.Name)
            .Select(x => new ProductDto
            {
                Name = x.Name,
                SKU = x.SKU,
                PurchasePrice = x.PurchasePrice,
                SellingPrice = x.SellingPrice,
                Stock = x.Stock
            })
            .ToListAsync();
    }

    public async Task<ProductDto?> GetByIdAsync(int id)
    {
        return await _context.Products
            .Where(x => x.Id == id && !x.IsDeleted)
            .Select(x => new ProductDto
            {
                Name = x.Name,
                SKU = x.SKU,
                PurchasePrice = x.PurchasePrice,
                SellingPrice = x.SellingPrice,
                Stock = x.Stock
            })
            .FirstOrDefaultAsync();
    }

    public async Task UpdateAsync(int id, ProductDto dto)
    {
        var product = await _context.Products.FindAsync(id);

        if (product == null || product.IsDeleted)
            throw new Exception("Product not found.");

        product.Name = dto.Name;
        product.SKU = dto.SKU;
        product.PurchasePrice = dto.PurchasePrice;
        product.SellingPrice = dto.SellingPrice;
        product.Stock = dto.Stock;

        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(int id)
    {
        var product = await _context.Products.FindAsync(id);

        if (product == null || product.IsDeleted)
            throw new Exception("Product not found.");

        product.IsDeleted = true;

        await _context.SaveChangesAsync();
    }
}