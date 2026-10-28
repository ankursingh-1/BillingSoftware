using System.Linq.Expressions;
using Billing.API.Models;
using Billing.Application.Interfaces;
using Billing.Domain.Entities;
using Billing.Domain.Enums;
using Billing.Persistence.Context;
using Billing.Shared.Exceptions;
using Microsoft.EntityFrameworkCore;

namespace Billing.API.Services;

public class ProductService
{
    private const int MaxPageSize = 100;

    private readonly BillingDbContext _context;
    private readonly IInventoryService _inventoryService;

    public ProductService(
        BillingDbContext context,
        IInventoryService inventoryService)
    {
        _context = context;
        _inventoryService = inventoryService;
    }

    private static readonly Expression<Func<Product, ProductDto>> ToDto = x => new ProductDto
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
        BrandName = x.Brand != null ? x.Brand.Name : null,
        UnitId = x.UnitId,
        UnitName = x.Unit != null ? x.Unit.Name : null,
        TaxId = x.TaxId,
        TaxName = x.Tax != null ? x.Tax.Name : null,
        TaxPercentage = x.Tax != null ? x.Tax.Percentage : null,
    };

    private async Task ValidateReferencesAsync(ProductDto dto)
    {
        if (dto.CategoryId.HasValue &&
            !await _context.Categories.AnyAsync(x => x.Id == dto.CategoryId && !x.IsDeleted))
            throw new BusinessException("Selected category was not found.");

        if (dto.BrandId.HasValue &&
            !await _context.Brands.AnyAsync(x => x.Id == dto.BrandId && !x.IsDeleted))
            throw new BusinessException("Selected brand was not found.");

        if (dto.UnitId.HasValue &&
            !await _context.Units.AnyAsync(x => x.Id == dto.UnitId && !x.IsDeleted))
            throw new BusinessException("Selected unit was not found.");

        if (dto.TaxId.HasValue &&
            !await _context.Taxes.AnyAsync(x => x.Id == dto.TaxId && !x.IsDeleted))
            throw new BusinessException("Selected tax was not found.");
    }

    public async Task<int> CreateAsync(ProductDto dto)
    {
        var name = dto.Name.Trim();
        var sku = dto.SKU.Trim();

        if (name.Length == 0 || sku.Length == 0)
            throw new BusinessException("Product name and SKU are required.");

        if (await _context.Products.AnyAsync(x => x.SKU == sku && !x.IsDeleted))
            throw BusinessException.Conflict("SKU already exists.");

        await ValidateReferencesAsync(dto);

        // Product and its opening-stock ledger entry succeed or fail together.
        await using var transaction = await _context.Database.BeginTransactionAsync();

        var product = new Product
        {
            Name = name,
            SKU = sku,
            PurchasePrice = dto.PurchasePrice,
            SellingPrice = dto.SellingPrice,
            Stock = 0,
            CategoryId = dto.CategoryId,
            BrandId = dto.BrandId,
            UnitId = dto.UnitId,
            TaxId = dto.TaxId,
        };

        _context.Products.Add(product);
        await _context.SaveChangesAsync();

        if (dto.Stock > 0)
        {
            await _inventoryService.AdjustStockAsync(
                product.Id,
                dto.Stock,
                StockTransactionType.OpeningStock,
                product.SKU,
                "Opening stock");

            await _context.SaveChangesAsync();
        }

        await transaction.CommitAsync();

        return product.Id;
    }

    public async Task<List<ProductDto>> GetAllAsync()
    {
        return await _context.Products
            .AsNoTracking()
            .Where(x => !x.IsDeleted)
            .OrderBy(x => x.Name)
            .Select(ToDto)
            .ToListAsync();
    }

    public async Task<ProductDto?> GetByIdAsync(int id)
    {
        return await _context.Products
            .AsNoTracking()
            .Where(x => x.Id == id && !x.IsDeleted)
            .Select(ToDto)
            .FirstOrDefaultAsync();
    }

    public async Task UpdateAsync(int id, ProductDto dto)
    {
        var name = dto.Name.Trim();
        var sku = dto.SKU.Trim();

        if (name.Length == 0 || sku.Length == 0)
            throw new BusinessException("Product name and SKU are required.");

        var product = await _context.Products.FindAsync(id);

        if (product == null || product.IsDeleted)
            throw BusinessException.NotFound("Product not found.");

        if (await _context.Products.AnyAsync(x =>
            x.Id != id &&
            x.SKU == sku &&
            !x.IsDeleted))
        {
            throw BusinessException.Conflict("SKU already exists.");
        }

        await ValidateReferencesAsync(dto);

        product.Name = name;
        product.SKU = sku;
        product.PurchasePrice = dto.PurchasePrice;
        product.SellingPrice = dto.SellingPrice;
        // Stock is intentionally NOT updated here. Changing it from a stale
        // client value would overwrite sales/purchases made in the meantime
        // and bypass the stock ledger. Use stock adjustments instead.
        product.CategoryId = dto.CategoryId;
        product.BrandId = dto.BrandId;
        product.UnitId = dto.UnitId;
        product.TaxId = dto.TaxId;

        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(int id)
    {
        var product = await _context.Products.FindAsync(id);

        if (product == null || product.IsDeleted)
            throw BusinessException.NotFound("Product not found.");

        // Soft delete: sales, purchases and the stock ledger keep their history.
        product.IsDeleted = true;

        await _context.SaveChangesAsync();
    }

    public async Task<PagedResult<ProductDto>> SearchAsync(ProductSearchRequest request)
    {
        int pageSize = Math.Clamp(request.PageSize, 1, MaxPageSize);
        int pageNumber = Math.Max(request.PageNumber, 1);

        var query = _context.Products
            .AsNoTracking()
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
            .ThenBy(x => x.Id)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .Select(ToDto)
            .ToListAsync();

        return new PagedResult<ProductDto>
        {
            TotalRecords = totalRecords,
            TotalPages = (int)Math.Ceiling((double)totalRecords / pageSize),
            PageNumber = pageNumber,
            PageSize = pageSize,
            Data = products
        };
    }
}
