using Billing.API.Services.ProductImport;
using Billing.Application.DTOs.Products;
using ClosedXML.Excel;

namespace Billing.Tests;

public class ProductImportTests
{
    private static ProductImportCatalog Catalog()
    {
        var catalog = new ProductImportCatalog();
        catalog.Categories["Electronics"] = 1;
        catalog.Brands["Generic"] = 2;
        catalog.Units["Pcs"] = 3;
        catalog.Taxes.Add(new ProductImportTax { Id = 9, Name = "GST 18", Percentage = 18 });
        catalog.ExistingSkus.Add("EXISTING-1");
        return catalog;
    }

    private static byte[] Sheet(params object[][] rows)
    {
        using var workbook = new XLWorkbook();
        var products = workbook.AddWorksheet(ProductImportWorkbook.ProductsSheet);
        var headers = ProductImportWorkbook.Headers;
        for (var i = 0; i < headers.Length; i++)
            products.Cell(1, i + 1).Value = headers[i];

        for (var r = 0; r < rows.Length; r++)
        {
            for (var c = 0; c < rows[r].Length; c++)
            {
                var value = rows[r][c];
                if (value is null) continue;
                if (value is int i) products.Cell(r + 2, c + 1).Value = i;
                else if (value is decimal d) products.Cell(r + 2, c + 1).Value = d;
                else if (value is double db) products.Cell(r + 2, c + 1).Value = db;
                else products.Cell(r + 2, c + 1).Value = value.ToString();
            }
        }

        workbook.AddWorksheet(ProductImportWorkbook.SampleSheet).Cell(1, 1).Value = "ignored";

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }

    private static List<ProductImportRowDto> Run(byte[] bytes, ProductImportCatalog? catalog = null)
    {
        ProductImportWorkbook.EnsureSupportedFile(bytes, "products.xlsx");
        var parsed = ProductImportWorkbook.ParseProductsSheet(bytes);
        return ProductImportWorkbook.Validate(parsed, catalog ?? Catalog());
    }

    [Fact]
    public void Template_is_a_valid_xlsx_and_sample_sheet_is_not_imported()
    {
        var bytes = ProductImportWorkbook.BuildTemplate(Catalog());
        ProductImportWorkbook.EnsureSupportedFile(bytes, "template.xlsx");

        var rows = ProductImportWorkbook.ParseProductsSheet(bytes);
        Assert.Empty(rows.Where(r => r.Sku.StartsWith("DEMO-")));
    }

    [Fact]
    public void Valid_row_is_accepted_and_lookups_resolve()
    {
        var bytes = Sheet(new object[]
        {
            "USB cable", "USB-01", "Electronics", "Generic", "Pcs", "GST 18", "8517", 80m, 120m, 5
        });

        var rows = Run(bytes);
        var row = Assert.Single(rows);
        Assert.Equal(ProductImportStatus.Valid, row.Status);
        Assert.Equal(1, row.CategoryId);
        Assert.Equal(2, row.BrandId);
        Assert.Equal(3, row.UnitId);
        Assert.Equal(9, row.TaxId);
        Assert.Equal(5, row.OpeningStock);
        Assert.Null(row.Error);
    }

    [Fact]
    public void Tax_rate_number_matches_a_single_tax()
    {
        var bytes = Sheet(new object[] { "A", "A-1", "", "", "", "18", "", 1m, 2m, 0 });
        var row = Assert.Single(Run(bytes));
        Assert.Equal(ProductImportStatus.Valid, row.Status);
        Assert.Equal(9, row.TaxId);
    }

    [Fact]
    public void Missing_name_or_sku_is_invalid()
    {
        var bytes = Sheet(new object[] { "", "X-1", "", "", "", "", "", 1m, 2m, 0 });
        var row = Assert.Single(Run(bytes));
        Assert.Equal(ProductImportStatus.Invalid, row.Status);
        Assert.Contains("name", row.Error, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Negative_price_is_invalid()
    {
        var bytes = Sheet(new object[] { "A", "A-1", "", "", "", "", "", -1m, 10m, 0 });
        var row = Assert.Single(Run(bytes));
        Assert.Equal(ProductImportStatus.Invalid, row.Status);
        Assert.Contains("negative", row.Error, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Price_with_three_decimals_is_invalid()
    {
        var bytes = Sheet(new object[] { "A", "A-1", "", "", "", "", "", 1.123m, 10m, 0 });
        var row = Assert.Single(Run(bytes));
        Assert.Equal(ProductImportStatus.Invalid, row.Status);
        Assert.Contains("2 decimal", row.Error, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Missing_category_is_invalid()
    {
        var bytes = Sheet(new object[] { "A", "A-1", "NoSuchCategory", "", "", "", "", 1m, 2m, 0 });
        var row = Assert.Single(Run(bytes));
        Assert.Equal(ProductImportStatus.Invalid, row.Status);
        Assert.Contains("Category", row.Error);
    }

    [Fact]
    public void Missing_or_inactive_tax_is_invalid()
    {
        var bytes = Sheet(new object[] { "A", "A-1", "", "", "", "GST 28", "", 1m, 2m, 0 });
        var row = Assert.Single(Run(bytes));
        Assert.Equal(ProductImportStatus.Invalid, row.Status);
        Assert.Contains("Tax", row.Error);
    }

    [Fact]
    public void Invalid_hsn_is_rejected()
    {
        var bytes = Sheet(new object[] { "A", "A-1", "", "", "", "", "12", 1m, 2m, 0 });
        var row = Assert.Single(Run(bytes));
        Assert.Equal(ProductImportStatus.Invalid, row.Status);
        Assert.Contains("HSN", row.Error);
    }

    [Fact]
    public void Duplicate_sku_is_reported_even_when_the_first_row_is_invalid()
    {
        var bytes = Sheet(
            new object[] { "", "SAME", "", "", "", "", "", 1m, 2m, 0 },
            new object[] { "B", "SAME", "", "", "", "", "", 3m, 4m, 0 });

        var rows = Run(bytes);
        Assert.Equal(ProductImportStatus.Invalid, rows[0].Status);
        Assert.Equal(ProductImportStatus.Duplicate, rows[1].Status);
    }

    [Fact]
    public void Duplicate_sku_in_file_skips_the_later_row()
    {
        var bytes = Sheet(
            new object[] { "A", "SAME", "", "", "", "", "", 1m, 2m, 0 },
            new object[] { "B", "SAME", "", "", "", "", "", 3m, 4m, 0 });

        var rows = Run(bytes);
        Assert.Equal(ProductImportStatus.Valid, rows[0].Status);
        Assert.Equal(ProductImportStatus.Duplicate, rows[1].Status);
        Assert.Contains("file", rows[1].Error, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Existing_sku_is_skipped_and_not_overwritten()
    {
        var bytes = Sheet(new object[] { "A", "EXISTING-1", "", "", "", "", "", 1m, 2m, 10 });
        var row = Assert.Single(Run(bytes));
        Assert.Equal(ProductImportStatus.Duplicate, row.Status);
        Assert.Contains("already exists", row.Error, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Duplicate_names_are_allowed_when_skus_differ()
    {
        var bytes = Sheet(
            new object[] { "Same name", "SKU-A", "", "", "", "", "", 1m, 2m, 0 },
            new object[] { "Same name", "SKU-B", "", "", "", "", "", 1m, 2m, 0 });

        var rows = Run(bytes);
        Assert.All(rows, r => Assert.Equal(ProductImportStatus.Valid, r.Status));
    }

    [Fact]
    public void Empty_rows_are_ignored()
    {
        var bytes = Sheet(
            new object[] { "", "", "", "", "", "", "", "", "", "" },
            new object[] { "A", "A-1", "", "", "", "", "", 1m, 2m, 0 });

        Assert.Single(Run(bytes));
    }

    [Fact]
    public void Sample_sheet_is_never_read_even_when_filled()
    {
        using var workbook = new XLWorkbook();
        var products = workbook.AddWorksheet("Products");
        products.Cell(1, 1).Value = "ProductName";
        products.Cell(1, 2).Value = "SKU";
        products.Cell(1, 8).Value = "PurchasePrice";
        products.Cell(1, 9).Value = "SellingPrice";
        var sample = workbook.AddWorksheet("Sample");
        sample.Cell(1, 1).Value = "ProductName";
        sample.Cell(1, 2).Value = "SKU";
        sample.Cell(2, 1).Value = "Should not import";
        sample.Cell(2, 2).Value = "NOPE";
        using var stream = new MemoryStream();
        workbook.SaveAs(stream);

        Assert.Empty(ProductImportWorkbook.ParseProductsSheet(stream.ToArray()));
    }

    [Fact]
    public void Rejects_files_that_are_not_xlsx()
    {
        var ex = Assert.Throws<InvalidOperationException>(() =>
            ProductImportWorkbook.EnsureSupportedFile(new byte[] { 1, 2, 3, 4 }, "x.xlsx"));
        Assert.Contains("xlsx", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Rejects_oversized_payload()
    {
        var huge = new byte[ProductImportWorkbook.MaxFileBytes + 1];
        huge[0] = (byte)'P';
        huge[1] = (byte)'K';
        var ex = Assert.Throws<InvalidOperationException>(() =>
            ProductImportWorkbook.EnsureSupportedFile(huge, "big.xlsx"));
        Assert.Contains("5 MB", ex.Message);
    }

    [Fact]
    public void Error_report_prefixes_formula_like_values()
    {
        var rows = new[]
        {
            new ProductImportRowDto
            {
                ExcelRow = 2, Name = "=CMD()", Sku = "+1", Error = "@sum", Status = "Invalid"
            }
        };

        Assert.StartsWith("'", ProductImportWorkbook.Sanitize("=CMD()"));
        Assert.StartsWith("'", ProductImportWorkbook.Sanitize("+1"));
        Assert.StartsWith("'", ProductImportWorkbook.Sanitize("@sum"));

        var bytes = ProductImportWorkbook.BuildResultReport(rows);
        using var stream = new MemoryStream(bytes);
        using var workbook = new XLWorkbook(stream);
        var sheet = workbook.Worksheet(1);
        Assert.False(sheet.Cell(2, 1).HasFormula);
        Assert.False(sheet.Cell(2, 2).HasFormula);
        Assert.False(sheet.Cell(2, 13).HasFormula);
        Assert.Equal("@", sheet.Cell(2, 1).Style.NumberFormat.Format);
    }

    [Fact]
    public void Guide_workbook_covers_valid_invalid_and_duplicate_rows()
    {
        var catalog = Catalog();
        var bytes = ProductImportWorkbook.BuildGuideWorkbook(catalog);
        var rows = Run(bytes, catalog);

        Assert.Contains(rows, r => r.Status == ProductImportStatus.Valid && r.Sku == "SAMPLE-USB-01");
        Assert.Contains(rows, r => r.Status == ProductImportStatus.Invalid);
        Assert.Contains(rows, r => r.Status == ProductImportStatus.Duplicate && r.Sku == "SAMPLE-USB-01");
        Assert.Contains(rows, r => r.Status == ProductImportStatus.Duplicate && r.Sku == "EXISTING-1");
        Assert.Empty(ProductImportWorkbook.ParseProductsSheet(bytes)
            .Where(r => r.Sku.StartsWith("DEMO-", StringComparison.OrdinalIgnoreCase)));

        TryWriteSampleWorkbook(bytes);
    }

    private static void TryWriteSampleWorkbook(byte[] bytes)
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir != null)
        {
            if (File.Exists(Path.Combine(dir.FullName, "Documentation", "PROJECT-STATUS.md")))
            {
                var samples = Path.Combine(dir.FullName, "Documentation", "samples");
                Directory.CreateDirectory(samples);
                File.WriteAllBytes(Path.Combine(samples, "product-import-test.xlsx"), bytes);
                return;
            }

            dir = dir.Parent;
        }
    }

    [Fact]
    public void Too_many_rows_are_rejected()
    {
        using var workbook = new XLWorkbook();
        var sheet = workbook.AddWorksheet("Products");
        sheet.Cell(1, 1).Value = "ProductName";
        sheet.Cell(1, 2).Value = "SKU";
        sheet.Cell(1, 8).Value = "PurchasePrice";
        sheet.Cell(1, 9).Value = "SellingPrice";
        for (var i = 0; i < ProductImportWorkbook.MaxRows + 1; i++)
        {
            sheet.Cell(i + 2, 1).Value = "P" + i;
            sheet.Cell(i + 2, 2).Value = "S" + i;
            sheet.Cell(i + 2, 8).Value = 1;
            sheet.Cell(i + 2, 9).Value = 2;
        }

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);

        var ex = Assert.Throws<InvalidOperationException>(() =>
            ProductImportWorkbook.ParseProductsSheet(stream.ToArray()));
        Assert.Contains("2,000", ex.Message);
    }
}
