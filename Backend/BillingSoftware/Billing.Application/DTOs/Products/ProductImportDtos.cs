namespace Billing.Application.DTOs.Products;

public static class ProductImportStatus
{
    public const string Valid = "Valid";
    public const string Invalid = "Invalid";
    public const string Duplicate = "Duplicate";
    public const string Imported = "Imported";
    public const string Skipped = "Skipped";
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
    public int OpeningStock { get; set; }
    public string Status { get; set; } = ProductImportStatus.Invalid;
    public string? Error { get; set; }

    public int? CategoryId { get; set; }
    public int? BrandId { get; set; }
    public int? UnitId { get; set; }
    public int? TaxId { get; set; }
}

public class ProductImportPreviewDto
{
    public int TotalRows { get; set; }
    public int Valid { get; set; }
    public int Invalid { get; set; }
    public int Duplicate { get; set; }
    public bool CanImport { get; set; }

    /// <summary>
    /// Human-readable policy shown on the import screen.
    /// Import of the valid rows is atomic (all succeed or none are written).
    /// </summary>
    public string Policy { get; set; } = string.Empty;

    public List<ProductImportRowDto> Rows { get; set; } = new();
}

public class ProductImportResultDto : ProductImportPreviewDto
{
    public int Imported { get; set; }
    public int Skipped { get; set; }
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
}

public class ProductImportTax
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal Percentage { get; set; }
}
