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
            Stock = dto.Stock,
            CategoryId = dto.CategoryId,
            BrandId = dto.BrandId

        };

        _context.Products.Add(product);

        await _context.SaveChangesAsync();

        return product.Id;
    }

    public async Task<List<ProductDto>> GetAllAsync()
    {
        return await _context.Products
            .Include(x => x.Category)
            .Include(x => x.Brand)
            .Where(x => !x.IsDeleted)
            .OrderBy(x => x.Name)
            .Select(x => new ProductDto
            {
                Id = x.Id,
                Name = x.Name,
                SKU = x.SKU,
                PurchasePrice = x.PurchasePrice,
                SellingPrice = x.SellingPrice,
                Stock = x.Stock,
                CategoryId = x.CategoryId,
                CategoryName = x.Category != null ? x.Category.Name : null,

                BrandId = x.BrandId,
                BrandName = x.Brand != null ? x.Brand.Name : null
            })
            .ToListAsync();
    }

    public async Task<ProductDto?> GetByIdAsync(int id)
    {
        return await _context.Products
            .Include(x => x.Category)
            .Include(x => x.Brand)
            .Where(x => x.Id == id && !x.IsDeleted)
            .Select(x => new ProductDto
            {
                Id = x.Id,
                Name = x.Name,
                SKU = x.SKU,
                PurchasePrice = x.PurchasePrice,
                SellingPrice = x.SellingPrice,
                Stock = x.Stock,
                CategoryId = x.CategoryId,
                CategoryName = x.Category != null ? x.Category.Name : null,
                BrandId = x.BrandId,
                BrandName = x.Brand != null ? x.Brand.Name : null
            })
            .FirstOrDefaultAsync();
    }

    public async Task UpdateAsync(int id, ProductDto dto)
    {
        if (await _context.Products.AnyAsync(x =>
            x.Id != id &&
            x.SKU == dto.SKU &&
            !x.IsDeleted))
        {
            throw new Exception("SKU already exists.");
        }

        var product = await _context.Products.FindAsync(id);

        if (product == null || product.IsDeleted)
            throw new Exception("Product not found.");

        product.Name = dto.Name;
        product.SKU = dto.SKU;
        product.PurchasePrice = dto.PurchasePrice;
        product.SellingPrice = dto.SellingPrice;
        product.Stock = dto.Stock;
        product.CategoryId = dto.CategoryId;
        product.BrandId = dto.BrandId;

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
    public async Task<PagedResult<ProductDto>> SearchAsync(ProductSearchRequest request)
    {
        var query = _context.Products
            .Include(x => x.Category)
            .Include(x => x.Brand)
            .Where(x => !x.IsDeleted);

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            string search = request.Search.Trim();

            query = query.Where(x =>
                x.Name.Contains(search) ||
                x.SKU.Contains(search));
        }

        int totalRecords = await query.CountAsync();

        var products = await query
            .OrderBy(x => x.Name)
            .Skip((request.PageNumber - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(x => new ProductDto
            {
                Id = x.Id,
                Name = x.Name,
                SKU = x.SKU,
                PurchasePrice = x.PurchasePrice,
                SellingPrice = x.SellingPrice,
                Stock = x.Stock,
                CategoryId = x.CategoryId,
                CategoryName = x.Category != null ? x.Category.Name : null,
                BrandId = x.BrandId,
                BrandName = x.Brand != null ? x.Brand.Name : null
            })
            .ToListAsync();

        return new PagedResult<ProductDto>
        {
            TotalRecords = totalRecords,
            TotalPages = (int)Math.Ceiling((double)totalRecords / request.PageSize),
            PageNumber = request.PageNumber,
            PageSize = request.PageSize,
            Data = products
        };
    }
}