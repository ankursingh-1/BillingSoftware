namespace Billing.Application.DTOs.Products;

public static class ProductImportStatus
{
    public const string Valid = "Valid";
    public const string Invalid = "Invalid";
    public const string Duplicate = "Duplicate";
    public const string Imported = "Imported";
    public const string Skipped = "Skipped";
    public const string Unchanged = "Unchanged";
    public const string Conflict = "Conflict";
    public const string Updated = "Updated";
    public const string Added = "Added";
    public const string StockIncreased = "StockIncreased";
    public const string Failed = "Failed";
}

public class ProductImportRowDto
{
    public int ExcelRow { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Sku { get; set; } = string.Empty;
    public string? Category { get; set; }
    public string? Brand { get; set; }
    public string? Unit { get; set; }
    public string? Tax { get; set; }
    public string? Hsn { get; set; }
    public decimal? PurchasePrice { get; set; }
    public decimal? SellingPrice { get; set; }

    /// <summary>
    /// Mode A desired on-hand (OpeningStock / Stock / StockQuantity).
    /// For existing products only the difference from current stock is posted.
    /// Null means the cell was blank — not an explicit zero.
    /// </summary>
    public int? StockQuantity { get; set; }

    /// <summary>
    /// Mode B incremental delivery quantity. Null means blank (no add).
    /// </summary>
    public int? AddStockQuantity { get; set; }

    /// <summary>
    /// Compatibility: absolute quantity when provided, otherwise 0.
    /// Blank is not treated as a stock instruction for existing products.
    /// </summary>
    public int OpeningStock
    {
        get => StockQuantity ?? 0;
        set => StockQuantity = value;
    }

    public string? Barcode { get; set; }
    public int? WarrantyDurationValue { get; set; }
    public string? WarrantyDurationUnit { get; set; }
    public bool RequiresSerialNumber { get; set; }
    public bool RequiresSerialProvided { get; set; }
    public int? ProductId { get; set; }
    public string? ReceiptId { get; set; }

    public string Status { get; set; } = ProductImportStatus.Invalid;
    public string? Error { get; set; }
    public string? Action { get; set; }
    public string? FieldChanges { get; set; }

    public int? MatchedProductId { get; set; }
    public int? CurrentStock { get; set; }
    public int ProposedStockChange { get; set; }
    public bool WillAddProduct { get; set; }
    public bool WillUpdateFields { get; set; }
    public bool WillIncreaseStock { get; set; }

    public int? CategoryId { get; set; }
    public int? BrandId { get; set; }
    public int? UnitId { get; set; }
    public int? TaxId { get; set; }

    public bool StockQuantityUnparsed { get; set; }
    public bool AddStockQuantityUnparsed { get; set; }
    public bool ProductIdUnparsed { get; set; }
}

public class ProductImportPreviewDto
{
    public int TotalRows { get; set; }
    public int Valid { get; set; }
    public int Invalid { get; set; }
    public int Duplicate { get; set; }
    public int Unchanged { get; set; }
    public int Conflict { get; set; }
    public bool CanImport { get; set; }

    /// <summary>
    /// Human-readable policy shown on the import screen.
    /// Import of the applicable rows is atomic (all succeed or none are written).
    /// </summary>
    public string Policy { get; set; } = string.Empty;

    public List<ProductImportRowDto> Rows { get; set; } = new();
}

public class ProductImportResultDto : ProductImportPreviewDto
{
    public int Imported { get; set; }
    public int Skipped { get; set; }
    public int Added { get; set; }
    public int Updated { get; set; }
    public int StockIncreased { get; set; }
    public bool Replayed { get; set; }
}

public class ProductImportCatalog
{
    public Dictionary<string, int> Categories { get; } =
        new(StringComparer.OrdinalIgnoreCase);

    public Dictionary<string, int> Brands { get; } =
        new(StringComparer.OrdinalIgnoreCase);

    public Dictionary<string, int> Units { get; } =
        new(StringComparer.OrdinalIgnoreCase);

    public List<ProductImportTax> Taxes { get; } = new();

    public HashSet<string> ExistingSkus { get; } =
        new(StringComparer.OrdinalIgnoreCase);

    public HashSet<string> ExistingBarcodes { get; } =
        new(StringComparer.OrdinalIgnoreCase);

    public List<ProductImportExisting> ExistingProducts { get; } = new();
}

public class ProductImportExisting
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Sku { get; set; } = string.Empty;
    public string? Barcode { get; set; }
    public decimal PurchasePrice { get; set; }
    public decimal SellingPrice { get; set; }
    public int Stock { get; set; }
    public string? HsnCode { get; set; }
    public int? CategoryId { get; set; }
    public string? CategoryName { get; set; }
    public int? BrandId { get; set; }
    public string? BrandName { get; set; }
    public int? UnitId { get; set; }
    public string? UnitName { get; set; }
    public int? TaxId { get; set; }
    public string? TaxName { get; set; }
    public int? WarrantyDurationValue { get; set; }
    public string? WarrantyDurationUnit { get; set; }
    public bool RequiresSerialNumber { get; set; }
}

public class ProductImportTax
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal Percentage { get; set; }
}
