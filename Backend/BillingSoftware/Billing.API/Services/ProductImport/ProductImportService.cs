using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Text.Json;
using Billing.API.Services;
using Billing.Application.DTOs.Products;
using Billing.Application.Interfaces;
using Billing.Domain.Entities;
using Billing.Domain.Enums;
using Billing.Persistence.Context;
using Billing.Shared.Exceptions;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace Billing.API.Services.ProductImport;

public class ProductImportService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false
    };

    private static readonly ConcurrentDictionary<string, SemaphoreSlim> ImportGates = new();
    private static readonly ConcurrentDictionary<int, SemaphoreSlim> ProductGates = new();

    private readonly BillingDbContext _context;
    private readonly IInventoryService _inventory;
    private readonly ICurrentUser _user;

    public ProductImportService(
        BillingDbContext context,
        IInventoryService inventory,
        ICurrentUser user)
    {
        _context = context;
        _inventory = inventory;
        _user = user;
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
                p.Id,
                p.Name,
                p.SKU,
                Category = p.Category != null ? p.Category.Name : null,
                Brand = p.Brand != null ? p.Brand.Name : null,
                Unit = p.Unit != null ? p.Unit.Name : null,
                Tax = p.Tax != null ? p.Tax.Name : null,
                p.HsnCode,
                p.PurchasePrice,
                p.SellingPrice,
                p.Stock,
                p.Barcode,
                p.WarrantyDurationValue,
                WarrantyUnit = p.WarrantyDurationUnit != null ? p.WarrantyDurationUnit.ToString() : null,
                p.RequiresSerialNumber
            })
            .ToListAsync();

        return ProductImportWorkbook.BuildExport(products.Select(p =>
            (p.Id, p.Name, p.SKU, p.Category, p.Brand, p.Unit, p.Tax, p.HsnCode,
                p.PurchasePrice, p.SellingPrice, p.Stock, p.Barcode, p.WarrantyDurationValue,
                p.WarrantyUnit, p.RequiresSerialNumber)));
    }

    public async Task<ProductImportPreviewDto> PreviewAsync(IFormFile file)
    {
        var (_, rows) = await ParseAndValidateAsync(file);
        return ProductImportWorkbook.Summarise(rows);
    }

    public async Task<byte[]> ErrorReportAsync(IFormFile file)
    {
        var (_, rows) = await ParseAndValidateAsync(file);
        var flagged = rows.Where(r =>
            r.Status is ProductImportStatus.Invalid
                or ProductImportStatus.Duplicate
                or ProductImportStatus.Conflict
                or ProductImportStatus.Failed).ToList();
        return ProductImportWorkbook.BuildResultReport(flagged);
    }

    public async Task<ProductImportResultDto> ImportAsync(IFormFile file)
    {
        var (bytes, rows) = await ParseAndValidateAsync(file);
        var hash = FileHash(bytes);
        var preview = ProductImportWorkbook.Summarise(rows);

        var gate = ImportGates.GetOrAdd(hash, _ => new SemaphoreSlim(1, 1));
        await gate.WaitAsync();
        try
        {
            var replayed = await TryLoadBatchAsync(hash);
            if (replayed != null)
                return replayed;

            if (preview.Valid == 0)
                return ProductImportPlanner.ToResult(rows);

            try
            {
                return await ApplyAsync(file.FileName, hash, rows);
            }
            catch (Exception ex) when (IsMissingImportTable(ex))
            {
                // The failed insert is still tracked; retry without the batch tables.
                _context.ChangeTracker.Clear();
                return await ApplyAsync(file.FileName, hash, rows, persistBatch: false);
            }
        }
        finally
        {
            gate.Release();
        }
    }

    private async Task<ProductImportResultDto> ApplyAsync(
        string? fileName,
        string hash,
        List<ProductImportRowDto> rows,
        bool persistBatch = true)
    {
        var valid = rows.Where(r => r.Status == ProductImportStatus.Valid).ToList();

        await using var transaction = await _context.Database.BeginTransactionAsync();

        try
        {
            ProductImportBatch? batch = null;
            if (persistBatch)
            {
                batch = new ProductImportBatch
                {
                    FileHash = hash,
                    FileName = Truncate(fileName, 260),
                    ResultJson = "{}",
                    CreatedOn = DateTime.UtcNow,
                    CreatedBy = _user.Email
                };
                _context.ProductImportBatches.Add(batch);
                await _context.SaveChangesAsync();
            }

            var created = new List<(Product Product, ProductImportRowDto Row)>();

            foreach (var row in valid)
            {
                if (!row.WillAddProduct)
                    continue;

                if (await _context.Products.AnyAsync(p => p.SKU == row.Sku && !p.IsDeleted))
                {
                    row.Status = ProductImportStatus.Conflict;
                    row.Error = "SKU already exists. Concurrent import; this row was not added.";
                    row.WillAddProduct = false;
                    row.WillIncreaseStock = false;
                    throw new BusinessException(
                        "A concurrent import created the same SKU. The file was not applied; retry the preview.");
                }

                var product = new Product
                {
                    Name = row.Name.Trim(),
                    SKU = row.Sku.Trim(),
                    PurchasePrice = row.PurchasePrice ?? 0,
                    SellingPrice = row.SellingPrice ?? 0,
                    Stock = 0,
                    HsnCode = string.IsNullOrWhiteSpace(row.Hsn) ? null : row.Hsn.Trim(),
                    Barcode = string.IsNullOrWhiteSpace(row.Barcode) ? null : row.Barcode.Trim(),
                    WarrantyDurationValue = row.WarrantyDurationValue,
                    WarrantyDurationUnit = ParseUnit(row.WarrantyDurationUnit),
                    RequiresSerialNumber = row.RequiresSerialNumber,
                    CategoryId = row.CategoryId,
                    BrandId = row.BrandId,
                    UnitId = row.UnitId,
                    TaxId = row.TaxId,
                    CreatedOn = DateTime.UtcNow,
                    CreatedBy = _user.Email
                };

                _context.Products.Add(product);
                created.Add((product, row));
            }

            await _context.SaveChangesAsync();

            foreach (var (product, row) in created)
            {
                row.MatchedProductId = product.Id;
                await WithProductGateAsync(product.Id, async () =>
                {
                    await LockProductRowAsync(product.Id);
                    await _context.Entry(product).ReloadAsync();
                    ProductImportPlanner.RecalculateTargetStock(row, product.Stock);

                    if (row.WillIncreaseStock && row.ProposedStockChange > 0)
                    {
                        await IncreaseStockAsync(
                            product.Id,
                            row.ProposedStockChange,
                            StockTransactionType.OpeningStock,
                            StockReference(hash, row, product.Id),
                            isNew: true,
                            persistBatch,
                            batch);
                    }

                    row.Status = ProductImportStatus.Added;
                    row.Error = null;
                    await _context.SaveChangesAsync();
                });
            }

            foreach (var row in valid.Where(r => !r.WillAddProduct))
            {
                if (row.MatchedProductId is not > 0)
                {
                    row.Status = ProductImportStatus.Failed;
                    row.Error = "The row matched no product to update.";
                    throw new BusinessException(row.Error);
                }

                var productId = row.MatchedProductId.Value;
                await WithProductGateAsync(productId, async () =>
                {
                    await ApplyExistingProductAsync(row, hash, persistBatch, batch);
                    await _context.SaveChangesAsync();
                });
            }

            var result = ProductImportPlanner.ToResult(rows);
            if (batch != null)
            {
                batch.ResultJson = JsonSerializer.Serialize(result, JsonOptions);
                await _context.SaveChangesAsync();
            }

            await transaction.CommitAsync();
            return result;
        }
        catch (DbUpdateException ex) when (IsUniqueBatch(ex))
        {
            await transaction.RollbackAsync();
            return await TryLoadBatchAsync(hash)
                ?? throw new BusinessException("This file is already being imported. Try again in a moment.");
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    private async Task ApplyExistingProductAsync(
        ProductImportRowDto row,
        string hash,
        bool persistBatch,
        ProductImportBatch? batch)
    {
        var product = await _context.Products.FirstOrDefaultAsync(p =>
            p.Id == row.MatchedProductId && !p.IsDeleted);

        if (product == null)
        {
            row.Status = ProductImportStatus.Failed;
            row.Error = "The matched product was deleted before import finished.";
            throw new BusinessException(row.Error);
        }

        await LockProductRowAsync(product.Id);
        await _context.Entry(product).ReloadAsync();
        ProductImportPlanner.RecalculateTargetStock(row, product.Stock);

        if (row.Status == ProductImportStatus.Conflict && !row.WillUpdateFields)
            return;

        if (row.WillUpdateFields)
            ApplyFieldUpdates(product, row);

        if (row.WillIncreaseStock && row.ProposedStockChange > 0)
        {
            await IncreaseStockAsync(
                product.Id,
                row.ProposedStockChange,
                StockTransactionType.Adjustment,
                StockReference(hash, row, product.Id),
                isNew: false,
                persistBatch,
                batch);
        }

        if (row.WillIncreaseStock && row.WillUpdateFields)
            row.Status = ProductImportStatus.Updated;
        else if (row.WillIncreaseStock)
            row.Status = ProductImportStatus.StockIncreased;
        else if (row.WillUpdateFields)
            row.Status = ProductImportStatus.Updated;
        else
            row.Status = ProductImportStatus.Unchanged;

        if (row.WillIncreaseStock || row.Error == null)
            row.Error = row.WillIncreaseStock ? null : row.Error;
    }

    private async Task WithProductGateAsync(int productId, Func<Task> work)
    {
        var gate = ProductGates.GetOrAdd(productId, _ => new SemaphoreSlim(1, 1));
        await gate.WaitAsync();
        try
        {
            await work();
        }
        finally
        {
            gate.Release();
        }
    }

    private async Task LockProductRowAsync(int productId)
    {
        if (!string.Equals(
                _context.Database.ProviderName,
                "Microsoft.EntityFrameworkCore.SqlServer",
                StringComparison.OrdinalIgnoreCase))
            return;

        await SqlTransactionHelper.LockProductAsync(_context, productId);
    }

    private async Task IncreaseStockAsync(
        int productId,
        int quantity,
        StockTransactionType type,
        string referenceNo,
        bool isNew,
        bool persistBatch,
        ProductImportBatch? batch)
    {
        if (await _context.StockLedgers.AnyAsync(x =>
            x.ProductId == productId && x.ReferenceNo == referenceNo && !x.IsDeleted))
        {
            return;
        }

        if (persistBatch && batch != null)
        {
            var receiptKey = referenceNo;
            if (await _context.ProductImportReceipts.AnyAsync(r => r.ReceiptKey == receiptKey))
                return;

            _context.ProductImportReceipts.Add(new ProductImportReceipt
            {
                BatchId = batch.Id,
                ProductId = productId,
                ReceiptKey = receiptKey,
                Quantity = quantity,
                CreatedOn = DateTime.UtcNow,
                CreatedBy = _user.Email
            });
        }

        var remarks = isNew
            ? "Opening stock (Excel import)"
            : type == StockTransactionType.Adjustment
                ? "Excel import (target stock difference)"
                : "Excel import";

        await _inventory.AdjustStockAsync(
            productId,
            quantity,
            type,
            referenceNo,
            remarks);
    }

    private static void ApplyFieldUpdates(Product product, ProductImportRowDto row)
    {
        if (!string.IsNullOrWhiteSpace(row.Name))
            product.Name = row.Name.Trim();
        if (!string.IsNullOrWhiteSpace(row.Sku))
            product.SKU = row.Sku.Trim();
        if (row.PurchasePrice.HasValue)
            product.PurchasePrice = row.PurchasePrice.Value;
        if (row.SellingPrice.HasValue)
            product.SellingPrice = row.SellingPrice.Value;
        if (row.Hsn != null)
            product.HsnCode = string.IsNullOrWhiteSpace(row.Hsn) ? product.HsnCode : row.Hsn.Trim();
        if (row.Barcode != null)
            product.Barcode = string.IsNullOrWhiteSpace(row.Barcode) ? product.Barcode : row.Barcode.Trim();
        if (row.WarrantyDurationValue.HasValue)
        {
            product.WarrantyDurationValue = row.WarrantyDurationValue;
            product.WarrantyDurationUnit = ParseUnit(row.WarrantyDurationUnit);
        }
        if (row.RequiresSerialProvided)
            product.RequiresSerialNumber = row.RequiresSerialNumber;
        if (row.CategoryId.HasValue)
            product.CategoryId = row.CategoryId;
        if (row.BrandId.HasValue)
            product.BrandId = row.BrandId;
        if (row.UnitId.HasValue)
            product.UnitId = row.UnitId;
        if (row.TaxId.HasValue)
            product.TaxId = row.TaxId;

        product.modifieson = DateTime.UtcNow;
    }

    private async Task<ProductImportResultDto?> TryLoadBatchAsync(string hash)
    {
        try
        {
            var json = await _context.ProductImportBatches
                .AsNoTracking()
                .Where(b => b.FileHash == hash)
                .Select(b => b.ResultJson)
                .FirstOrDefaultAsync();

            if (string.IsNullOrWhiteSpace(json) || json == "{}")
                return null;

            var result = JsonSerializer.Deserialize<ProductImportResultDto>(json, JsonOptions);
            if (result == null)
                return null;

            result.Replayed = true;
            return result;
        }
        catch (Exception ex) when (IsMissingImportTable(ex))
        {
            return null;
        }
    }

    private async Task<(byte[] Bytes, List<ProductImportRowDto> Rows)> ParseAndValidateAsync(IFormFile file)
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
            return (bytes, ProductImportWorkbook.Validate(parsed, catalog));
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

        var products = await _context.Products.AsNoTracking()
            .Where(x => !x.IsDeleted)
            .Select(p => new ProductImportExisting
            {
                Id = p.Id,
                Name = p.Name,
                Sku = p.SKU,
                Barcode = p.Barcode,
                PurchasePrice = p.PurchasePrice,
                SellingPrice = p.SellingPrice,
                Stock = p.Stock,
                HsnCode = p.HsnCode,
                CategoryId = p.CategoryId,
                CategoryName = p.Category != null ? p.Category.Name : null,
                BrandId = p.BrandId,
                BrandName = p.Brand != null ? p.Brand.Name : null,
                UnitId = p.UnitId,
                UnitName = p.Unit != null ? p.Unit.Name : null,
                TaxId = p.TaxId,
                TaxName = p.Tax != null ? p.Tax.Name : null,
                WarrantyDurationValue = p.WarrantyDurationValue,
                WarrantyDurationUnit = p.WarrantyDurationUnit != null ? p.WarrantyDurationUnit.ToString() : null,
                RequiresSerialNumber = p.RequiresSerialNumber
            })
            .ToListAsync();

        catalog.ExistingProducts.AddRange(products);
        foreach (var product in products)
        {
            catalog.ExistingSkus.Add(product.Sku);
            if (!string.IsNullOrWhiteSpace(product.Barcode))
                catalog.ExistingBarcodes.Add(product.Barcode);
        }

        return catalog;
    }

    internal static string FileHash(byte[] bytes) =>
        Convert.ToHexString(SHA256.HashData(bytes));

    internal static string StockReference(string fileHash, ProductImportRowDto row, int productId)
    {
        if (!string.IsNullOrWhiteSpace(row.ReceiptId))
            return Truncate($"RCPT:{row.ReceiptId.Trim()}|{productId}", 100);

        var mode = row.AddStockQuantity.HasValue ? "A" : "S";
        var qty = row.AddStockQuantity ?? row.ProposedStockChange;
        return Truncate($"XLS:{fileHash[..Math.Min(16, fileHash.Length)]}|{productId}|{mode}|{qty}", 100);
    }

    private static bool IsUniqueBatch(DbUpdateException ex) =>
        ex.InnerException is SqlException sql && (sql.Number is 2601 or 2627);

    private static bool IsMissingImportTable(Exception ex)
    {
        for (var current = ex; current != null; current = current.InnerException)
        {
            if (current is SqlException sql
                && sql.Message.Contains("ProductImportBatch", StringComparison.OrdinalIgnoreCase))
                return true;
            if (current.Message.Contains("ProductImportBatch", StringComparison.OrdinalIgnoreCase)
                && current.Message.Contains("Invalid object", StringComparison.OrdinalIgnoreCase))
                return true;
        }

        return false;
    }

    private static string Truncate(string? value, int max)
    {
        if (string.IsNullOrEmpty(value))
            return "";
        return value.Length <= max ? value : value[..max];
    }

    private static WarrantyDurationUnit? ParseUnit(string? unit) =>
        string.IsNullOrWhiteSpace(unit)
            ? null
            : Enum.Parse<WarrantyDurationUnit>(unit, true);
}
