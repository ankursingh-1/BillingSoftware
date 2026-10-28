using Billing.Application.DTOs.Products;
using Billing.Application.Interfaces;
using Billing.Domain.Entities;
using Billing.Domain.Enums;
using Billing.Persistence.Context;
using Billing.Shared.Exceptions;
using Microsoft.EntityFrameworkCore;

namespace Billing.API.Services.ProductImport;

public class ProductImportService
{
    private readonly BillingDbContext _context;
    private readonly IInventoryService _inventory;

    public ProductImportService(BillingDbContext context, IInventoryService inventory)
    {
        _context = context;
        _inventory = inventory;
    }

    public async Task<byte[]> TemplateAsync()
    {
        var catalog = await LoadCatalogAsync();
        return ProductImportWorkbook.BuildTemplate(catalog);
    }

    public async Task<byte[]> ExportAsync()
    {
        var products = await _context.Products
            .AsNoTracking()
            .Where(p => !p.IsDeleted)
            .OrderBy(p => p.Name)
            .ThenBy(p => p.Id)
            .Select(p => new
            {
                p.Name,
                p.SKU,
                Category = p.Category != null ? p.Category.Name : null,
                Brand = p.Brand != null ? p.Brand.Name : null,
                Unit = p.Unit != null ? p.Unit.Name : null,
                Tax = p.Tax != null ? p.Tax.Name : null,
                p.HsnCode,
                p.PurchasePrice,
                p.SellingPrice,
                p.Stock
            })
            .ToListAsync();

        return ProductImportWorkbook.BuildExport(products.Select(p =>
            (p.Name, p.SKU, p.Category, p.Brand, p.Unit, p.Tax, p.HsnCode,
                p.PurchasePrice, p.SellingPrice, p.Stock)));
    }

    public async Task<ProductImportPreviewDto> PreviewAsync(IFormFile file)
    {
        var rows = await ParseAndValidateAsync(file);
        return ProductImportWorkbook.Summarise(rows);
    }

    public async Task<byte[]> ErrorReportAsync(IFormFile file)
    {
        var rows = await ParseAndValidateAsync(file);
        var flagged = rows.Where(r => r.Status != ProductImportStatus.Valid).ToList();
        return ProductImportWorkbook.BuildResultReport(flagged);
    }

    public async Task<ProductImportResultDto> ImportAsync(IFormFile file)
    {
        var rows = await ParseAndValidateAsync(file);
        var preview = ProductImportWorkbook.Summarise(rows);

        if (preview.Valid == 0)
        {
            return ToResult(preview, imported: 0);
        }

        var valid = rows.Where(r => r.Status == ProductImportStatus.Valid).ToList();

        await using var transaction = await _context.Database.BeginTransactionAsync();

        try
        {
            var created = new List<(Product Product, int Opening)>();

            foreach (var row in valid)
            {
                // Re-check inside the transaction so a concurrent create cannot
                // be overwritten and stock is not added twice on retry.
                if (await _context.Products.AnyAsync(p => p.SKU == row.Sku && !p.IsDeleted))
                {
                    row.Status = ProductImportStatus.Duplicate;
                    row.Error = "SKU already exists. Existing products are not overwritten.";
                    continue;
                }

                var product = new Product
                {
                    Name = row.Name.Trim(),
                    SKU = row.Sku.Trim(),
                    PurchasePrice = row.PurchasePrice ?? 0,
                    SellingPrice = row.SellingPrice ?? 0,
                    Stock = 0,
                    HsnCode = string.IsNullOrWhiteSpace(row.Hsn) ? null : row.Hsn.Trim(),
                    CategoryId = row.CategoryId,
                    BrandId = row.BrandId,
                    UnitId = row.UnitId,
                    TaxId = row.TaxId
                };

                _context.Products.Add(product);
                created.Add((product, row.OpeningStock));
                row.Status = ProductImportStatus.Imported;
                row.Error = null;
            }

            await _context.SaveChangesAsync();

            foreach (var (product, opening) in created)
            {
                if (opening > 0)
                {
                    await _inventory.AdjustStockAsync(
                        product.Id,
                        opening,
                        StockTransactionType.OpeningStock,
                        product.SKU,
                        "Opening stock (Excel import)");
                }
            }

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }

        var imported = rows.Count(r => r.Status == ProductImportStatus.Imported);
        var result = ToResult(ProductImportWorkbook.Summarise(rows), imported);
        result.Imported = imported;
        result.Skipped = rows.Count(r =>
            r.Status is ProductImportStatus.Invalid or ProductImportStatus.Duplicate);
        return result;
    }

    private static ProductImportResultDto ToResult(ProductImportPreviewDto preview, int imported) => new()
    {
        TotalRows = preview.TotalRows,
        Valid = preview.Valid,
        Invalid = preview.Invalid,
        Duplicate = preview.Duplicate,
        CanImport = false,
        Policy = preview.Policy,
        Rows = preview.Rows,
        Imported = imported,
        Skipped = preview.Invalid + preview.Duplicate
    };

    private async Task<List<ProductImportRowDto>> ParseAndValidateAsync(IFormFile file)
    {
        if (file == null || file.Length == 0)
            throw new BusinessException("Choose an .xlsx file to upload.");

        if (file.Length > ProductImportWorkbook.MaxFileBytes)
            throw new BusinessException("The file is larger than 5 MB.");

        await using var input = file.OpenReadStream();
        var bytes = new byte[file.Length];
        var read = 0;
        while (read < bytes.Length)
        {
            var n = await input.ReadAsync(bytes.AsMemory(read, bytes.Length - read));
            if (n == 0) break;
            read += n;
        }

        try
        {
            ProductImportWorkbook.EnsureSupportedFile(bytes, file.FileName);
            var parsed = ProductImportWorkbook.ParseProductsSheet(bytes);
            var catalog = await LoadCatalogAsync();
            return ProductImportWorkbook.Validate(parsed, catalog);
        }
        catch (InvalidOperationException ex)
        {
            throw new BusinessException(ex.Message);
        }
        catch (Exception ex) when (ex is not BusinessException)
        {
            throw new BusinessException("The Excel file could not be read. Download the template and try again.");
        }
    }

    private async Task<ProductImportCatalog> LoadCatalogAsync()
    {
        var catalog = new ProductImportCatalog();

        var categories = await _context.Categories.AsNoTracking()
            .Where(x => !x.IsDeleted && x.IsActive)
            .Select(x => new { x.Id, x.Name })
            .ToListAsync();
        foreach (var item in categories)
            catalog.Categories[item.Name] = item.Id;

        var brands = await _context.Brands.AsNoTracking()
            .Where(x => !x.IsDeleted && x.IsActive)
            .Select(x => new { x.Id, x.Name })
            .ToListAsync();
        foreach (var item in brands)
            catalog.Brands[item.Name] = item.Id;

        var units = await _context.Units.AsNoTracking()
            .Where(x => !x.IsDeleted && x.IsActive)
            .Select(x => new { x.Id, x.Name })
            .ToListAsync();
        foreach (var item in units)
            catalog.Units[item.Name] = item.Id;

        catalog.Taxes.AddRange(await _context.Taxes.AsNoTracking()
            .Where(x => !x.IsDeleted && x.IsActive)
            .Select(x => new ProductImportTax { Id = x.Id, Name = x.Name, Percentage = x.Percentage })
            .ToListAsync());

        foreach (var sku in await _context.Products.AsNoTracking()
            .Where(x => !x.IsDeleted)
            .Select(x => x.SKU)
            .ToListAsync())
        {
            catalog.ExistingSkus.Add(sku);
        }

        return catalog;
    }
}
