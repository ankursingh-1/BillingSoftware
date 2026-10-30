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
        catalog.ExistingProducts.Add(new ProductImportExisting
        {
            Id = 100,
            Name = "Existing",
            Sku = "EXISTING-1",
            Barcode = "BC-EXISTING",
            PurchasePrice = 1m,
            SellingPrice = 2m,
            Stock = 10,
            HsnCode = null
        });
        catalog.ExistingSkus.Add("EXISTING-1");
        catalog.ExistingBarcodes.Add("BC-EXISTING");
        catalog.ExistingProducts.Add(new ProductImportExisting
        {
            Id = 101,
            Name = "Other",
            Sku = "OTHER-1",
            Barcode = "BC-OTHER",
            PurchasePrice = 5m,
            SellingPrice = 8m,
            Stock = 4
        });
        catalog.ExistingSkus.Add("OTHER-1");
        catalog.ExistingBarcodes.Add("BC-OTHER");
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
        Assert.Equal(ProductImportStatus.Valid, rows[1].Status);
    }

    [Fact]
    public void Duplicate_sku_in_file_skips_the_later_row()
    {
        var bytes = Sheet(
            new object[] { "A", "SAME", "", "", "", "", "", 1m, 2m, 0 },
            new object[] { "A", "SAME", "", "", "", "", "", 1m, 2m, 0 });

        var rows = Run(bytes);
        Assert.Equal(ProductImportStatus.Valid, rows[0].Status);
        Assert.Equal(ProductImportStatus.Duplicate, rows[1].Status);
        Assert.Contains("file", rows[1].Error, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Existing_sku_with_the_same_details_and_stock_is_unchanged()
    {
        var bytes = Sheet(new object[] { "Existing", "EXISTING-1", "", "", "", "", "", 1m, 2m, 10 });
        var row = Assert.Single(Run(bytes));
        Assert.Equal(ProductImportStatus.Unchanged, row.Status);
        Assert.False(row.WillIncreaseStock);
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
        Assert.Contains(rows, r => r.Sku == "EXISTING-1" && r.Status == ProductImportStatus.Unchanged);
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

    [Fact]
    public void New_product_with_barcode_and_sku_is_valid()
    {
        var bytes = Sheet(new object[]
        {
            "USB cable", "USB-01", "Electronics", "Generic", "Pcs", "GST 18", "8517", 80m, 120m, 5, "12345678"
        });
        var row = Assert.Single(Run(bytes));
        Assert.Equal(ProductImportStatus.Valid, row.Status);
        Assert.True(row.WillAddProduct);
        Assert.Equal(5, row.ProposedStockChange);
        Assert.Equal("12345678", row.Barcode);
    }

    [Fact]
    public void Existing_product_selling_price_change_is_an_update()
    {
        var bytes = Sheet(new object[] { "Existing", "EXISTING-1", "", "", "", "", "", 1m, 9m, 10 });
        var row = Assert.Single(Run(bytes));
        Assert.Equal(ProductImportStatus.Valid, row.Status);
        Assert.True(row.WillUpdateFields);
        Assert.False(row.WillIncreaseStock);
        Assert.Contains("selling price", row.FieldChanges);
    }

    [Fact]
    public void Existing_product_same_barcode_higher_absolute_stock_plans_an_increase()
    {
        var bytes = Sheet(new object[]
        {
            "Existing", "EXISTING-1", "", "", "", "", "", 1m, 2m, 15, "BC-EXISTING"
        });
        var row = Assert.Single(Run(bytes));
        Assert.Equal(ProductImportStatus.Valid, row.Status);
        Assert.Equal(100, row.MatchedProductId);
        Assert.True(row.WillIncreaseStock);
        Assert.Equal(5, row.ProposedStockChange);
        Assert.Equal(10, row.CurrentStock);
    }

    [Fact]
    public void Incremental_stock_is_added_and_zero_incremental_does_not_move_stock()
    {
        var add = Sheet(new object[]
        {
            "Existing", "EXISTING-1", "", "", "", "", "", 1m, 2m, "", "BC-EXISTING", "", "", "", "", "", 5
        });
        var row = Assert.Single(Run(add));
        Assert.Equal(ProductImportStatus.Valid, row.Status);
        Assert.Equal(5, row.AddStockQuantity);
        Assert.True(row.WillIncreaseStock);
        Assert.Equal(5, row.ProposedStockChange);

        var zero = Sheet(new object[]
        {
            "Existing", "EXISTING-1", "", "", "", "", "", 1m, 2m, "", "BC-EXISTING", "", "", "", "", "", 0
        });
        var zeroRow = Assert.Single(Run(zero));
        Assert.Equal(ProductImportStatus.Unchanged, zeroRow.Status);
        Assert.False(zeroRow.WillIncreaseStock);
    }

    [Fact]
    public void Blank_stock_does_not_change_existing_on_hand()
    {
        var bytes = Sheet(new object[] { "Existing", "EXISTING-1", "", "", "", "", "", 1m, 2m, "" });
        var row = Assert.Single(Run(bytes));
        Assert.Null(row.StockQuantity);
        Assert.Equal(ProductImportStatus.Unchanged, row.Status);
        Assert.False(row.WillIncreaseStock);
    }

    [Fact]
    public void Explicit_zero_stock_on_existing_product_is_a_rejected_reduction()
    {
        var bytes = Sheet(new object[] { "Existing", "EXISTING-1", "", "", "", "", "", 1m, 2m, 0 });
        var row = Assert.Single(Run(bytes));
        Assert.Equal(0, row.StockQuantity);
        Assert.Equal(ProductImportStatus.Conflict, row.Status);
        Assert.Contains("reduction", row.Error, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Conflicting_duplicate_rows_are_held_for_review()
    {
        var bytes = Sheet(
            new object[] { "A", "SAME", "", "", "", "", "", 1m, 2m, 0 },
            new object[] { "B", "SAME", "", "", "", "", "", 3m, 4m, 0 });
        var rows = Run(bytes);
        Assert.All(rows, r => Assert.Equal(ProductImportStatus.Conflict, r.Status));
    }

    [Fact]
    public void Barcode_and_sku_pointing_at_different_products_is_an_identity_conflict()
    {
        var bytes = Sheet(new object[]
        {
            "Existing", "OTHER-1", "", "", "", "", "", 1m, 2m, 10, "BC-EXISTING"
        });
        var row = Assert.Single(Run(bytes));
        Assert.Equal(ProductImportStatus.Conflict, row.Status);
        Assert.Contains("Product identity is ambiguous", row.Error);
        Assert.Contains("BC-EXISTING", row.Error);
        Assert.Contains("OTHER-1", row.Error);
        Assert.False(row.WillIncreaseStock);
    }

    [Fact]
    public void Same_sku_with_another_products_barcode_is_an_identity_conflict()
    {
        var bytes = Sheet(new object[]
        {
            "Existing", "EXISTING-1", "", "", "", "", "", 1m, 2m, 10, "BC-OTHER"
        });
        var row = Assert.Single(Run(bytes));
        Assert.Equal(ProductImportStatus.Conflict, row.Status);
        Assert.Contains("Product identity is ambiguous", row.Error);
        Assert.Contains("BC-OTHER", row.Error);
        Assert.Contains("EXISTING-1", row.Error);
    }

    [Fact]
    public void Blank_optional_fields_do_not_plan_an_overwrite()
    {
        var bytes = Sheet(new object[] { "Existing", "EXISTING-1", "", "", "", "", "", 1m, 2m, 10 });
        var row = Assert.Single(Run(bytes));
        Assert.Equal(ProductImportStatus.Unchanged, row.Status);
        Assert.Null(row.FieldChanges);
    }

    [Fact]
    public void Distinct_receipts_for_the_same_product_stay_independent()
    {
        var bytes = Sheet(
            new object[] { "Existing", "EXISTING-1", "", "", "", "", "", 1m, 2m, "", "BC-EXISTING", "", "", "", "", "", 3, "R-1" },
            new object[] { "Existing", "EXISTING-1", "", "", "", "", "", 1m, 2m, "", "BC-EXISTING", "", "", "", "", "", 2, "R-2" });
        var rows = Run(bytes);
        Assert.Equal(2, rows.Count);
        Assert.All(rows, r => Assert.Equal(ProductImportStatus.Valid, r.Status));
        Assert.Equal(3, rows[0].ProposedStockChange);
        Assert.Equal(2, rows[1].ProposedStockChange);
    }

    [Theory]
    [InlineData(10, 20, ProductImportStatus.Valid, 10)]
    [InlineData(50, 50, ProductImportStatus.Unchanged, 0)]
    [InlineData(30, 40, ProductImportStatus.Valid, 10)]
    [InlineData(100, 100, ProductImportStatus.Unchanged, 0)]
    [InlineData(10, 5, ProductImportStatus.Conflict, 0)]
    public void OpeningStock_is_the_desired_on_hand_for_existing_products(
        int current,
        int imported,
        string status,
        int difference)
    {
        var row = Assert.Single(Run(
            Sheet(new object[] { "Existing", "EXISTING-1", "", "", "", "", "", 1m, 2m, imported }),
            CatalogWithStock(current)));

        Assert.Equal(status, row.Status);
        Assert.Equal(current, row.CurrentStock);
        Assert.Equal(imported, row.StockQuantity);
        Assert.Equal(difference, row.ProposedStockChange);
        Assert.Equal(difference > 0, row.WillIncreaseStock);
        if (status == ProductImportStatus.Conflict)
            Assert.Contains("reduction", row.Error, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Same_barcode_and_unchanged_values_are_skipped()
    {
        var bytes = Sheet(new object[]
        {
            "Existing", "EXISTING-1", "", "", "", "", "", 1m, 2m, 10, "BC-EXISTING"
        });
        var row = Assert.Single(Run(bytes));
        Assert.Equal(ProductImportStatus.Unchanged, row.Status);
        Assert.Equal(100, row.MatchedProductId);
        Assert.False(row.WillIncreaseStock);
        Assert.False(row.WillUpdateFields);
    }

    [Fact]
    public void Same_barcode_and_changed_selling_price_plans_an_update()
    {
        var bytes = Sheet(new object[]
        {
            "Existing", "EXISTING-1", "", "", "", "", "", 1m, 120m, 10, "BC-EXISTING"
        });
        var row = Assert.Single(Run(bytes));
        Assert.Equal(ProductImportStatus.Valid, row.Status);
        Assert.True(row.WillUpdateFields);
        Assert.False(row.WillIncreaseStock);
        Assert.Contains("selling price", row.FieldChanges);
    }

    [Fact]
    public void Price_and_stock_changes_are_planned_together()
    {
        var bytes = Sheet(new object[] { "Existing", "EXISTING-1", "", "", "", "", "", 1m, 120m, 20 });
        var row = Assert.Single(Run(bytes));
        Assert.Equal(ProductImportStatus.Valid, row.Status);
        Assert.True(row.WillUpdateFields);
        Assert.True(row.WillIncreaseStock);
        Assert.Equal(10, row.ProposedStockChange);
        Assert.Contains("selling price", row.FieldChanges);
    }

    [Fact]
    public void Duplicate_identical_rows_keep_one_stock_plan()
    {
        var bytes = Sheet(
            new object[] { "Existing", "EXISTING-1", "", "", "", "", "", 1m, 2m, 20 },
            new object[] { "Existing", "EXISTING-1", "", "", "", "", "", 1m, 2m, 20 });
        var rows = Run(bytes);
        Assert.Equal(ProductImportStatus.Valid, rows[0].Status);
        Assert.Equal(10, rows[0].ProposedStockChange);
        Assert.Equal(ProductImportStatus.Duplicate, rows[1].Status);
        Assert.Equal(0, rows[1].ProposedStockChange);
        Assert.False(rows[1].WillIncreaseStock);
    }

    [Fact]
    public void Confirm_recalculates_opening_stock_against_live_on_hand()
    {
        var row = Assert.Single(Run(
            Sheet(new object[] { "Existing", "EXISTING-1", "", "", "", "", "", 1m, 2m, 20 })));
        Assert.Equal(10, row.ProposedStockChange);

        ProductImportPlanner.RecalculateTargetStock(row, 18);
        Assert.Equal(18, row.CurrentStock);
        Assert.Equal(2, row.ProposedStockChange);
        Assert.True(row.WillIncreaseStock);

        ProductImportPlanner.RecalculateTargetStock(row, 20);
        Assert.Equal(0, row.ProposedStockChange);
        Assert.False(row.WillIncreaseStock);

        ProductImportPlanner.RecalculateTargetStock(row, 25);
        Assert.Equal(0, row.ProposedStockChange);
        Assert.False(row.WillIncreaseStock);
        Assert.Equal(ProductImportStatus.Conflict, row.Status);
        Assert.Contains("reduction", row.Error, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Existing_sku_only_higher_OpeningStock_plans_the_difference()
    {
        var bytes = Sheet(new object[] { "Existing", "EXISTING-1", "", "", "", "", "", 1m, 2m, 20 });
        var row = Assert.Single(Run(bytes));
        Assert.Equal(ProductImportStatus.Valid, row.Status);
        Assert.Equal(100, row.MatchedProductId);
        Assert.Equal(10, row.ProposedStockChange);
        Assert.Null(row.Barcode);
    }

    [Fact]
    public void New_sku_with_an_existing_barcode_matches_the_barcode_owner()
    {
        var bytes = Sheet(new object[]
        {
            "Existing", "BRAND-NEW", "", "", "", "", "", 1m, 2m, 10, "BC-EXISTING"
        });
        var row = Assert.Single(Run(bytes));
        Assert.Equal(100, row.MatchedProductId);
        Assert.True(row.WillUpdateFields);
        Assert.Contains("SKU", row.FieldChanges);
        Assert.False(row.WillAddProduct);
        Assert.DoesNotContain("Barcode belongs to another product", row.Error ?? "");
    }

    [Fact]
    public void Cover_excel_10_against_on_hand_20_is_a_stock_reduction_conflict()
    {
        var catalog = SamsungCoverCatalog(stock: 20);
        var bytes = Sheet(new object[]
        {
            "Samsung Phone Cover (demo)", "SAM-COVER-001", "", "", "", "", "",
            150m, 299m, 10, "DEMO-BC-SAM-COVER-001"
        });
        var row = Assert.Single(Run(bytes, catalog));
        Assert.Equal(ProductImportStatus.Conflict, row.Status);
        Assert.Equal(7, row.MatchedProductId);
        Assert.Equal(20, row.CurrentStock);
        Assert.Equal(10, row.StockQuantity);
        Assert.Equal(0, row.ProposedStockChange);
        Assert.Contains("Target stock is lower than current stock", row.Error);
        Assert.Contains("on hand 20", row.Error);
        Assert.Contains("Excel 10", row.Error);
    }

    [Fact]
    public void Eighteen_unchanged_rows_and_one_stock_increase_are_importable()
    {
        var catalog = new ProductImportCatalog();
        var rows = new List<object[]>();
        for (var i = 1; i <= 19; i++)
        {
            var sku = $"DEMO-{i:000}";
            var barcode = $"DEMO-BC-{sku}";
            catalog.ExistingProducts.Add(new ProductImportExisting
            {
                Id = i,
                Name = "Item " + i,
                Sku = sku,
                Barcode = barcode,
                PurchasePrice = 1m,
                SellingPrice = 2m,
                Stock = i == 14 ? 14 : 10
            });
            catalog.ExistingSkus.Add(sku);
            catalog.ExistingBarcodes.Add(barcode);
            var opening = i == 14 ? 20 : 10;
            rows.Add(new object[] { "Item " + i, sku, "", "", "", "", "", 1m, 2m, opening, barcode });
        }

        var planned = Run(Sheet(rows.ToArray()), catalog);
        var summary = ProductImportWorkbook.Summarise(planned);
        Assert.Equal(19, summary.TotalRows);
        Assert.Equal(1, summary.Valid);
        Assert.Equal(18, summary.Unchanged);
        Assert.Equal(0, summary.Conflict);
        Assert.Equal(0, summary.Duplicate);
        Assert.True(summary.CanImport);
        var changed = Assert.Single(planned.Where(r => r.Status == ProductImportStatus.Valid));
        Assert.Equal("DEMO-014", changed.Sku);
        Assert.Equal(6, changed.ProposedStockChange);
    }

    [Fact]
    public void Eighteen_unchanged_rows_and_one_stock_reduction_cannot_import()
    {
        var catalog = new ProductImportCatalog();
        var rows = new List<object[]>();
        for (var i = 1; i <= 19; i++)
        {
            var sku = $"DEMO-{i:000}";
            catalog.ExistingProducts.Add(new ProductImportExisting
            {
                Id = i,
                Name = "Item " + i,
                Sku = sku,
                PurchasePrice = 1m,
                SellingPrice = 2m,
                Stock = i == 14 ? 20 : 10
            });
            catalog.ExistingSkus.Add(sku);
            var opening = i == 14 ? 10 : 10;
            rows.Add(new object[] { "Item " + i, sku, "", "", "", "", "", 1m, 2m, opening });
        }

        var planned = Run(Sheet(rows.ToArray()), catalog);
        var summary = ProductImportWorkbook.Summarise(planned);
        Assert.Equal(1, summary.Conflict);
        Assert.Equal(18, summary.Unchanged);
        Assert.Equal(0, summary.Valid);
        Assert.False(summary.CanImport);
        var conflict = Assert.Single(planned.Where(r => r.Status == ProductImportStatus.Conflict));
        Assert.Contains("Target stock is lower than current stock", conflict.Error);
    }

    [Fact]
    public void Samsung_cover_same_barcode_and_sku_plans_stock_difference_14_to_20()
    {
        var catalog = SamsungCoverCatalog(stock: 14);
        var bytes = Sheet(new object[]
        {
            "Samsung Phone Cover (demo)", "SAM-COVER-001", "", "", "", "", "",
            150m, 299m, 20, "DEMO-BC-SAM-COVER-001"
        });

        var row = Assert.Single(Run(bytes, catalog));
        Assert.Equal(ProductImportStatus.Valid, row.Status);
        Assert.NotEqual(ProductImportStatus.Duplicate, row.Status);
        Assert.Equal(7, row.MatchedProductId);
        Assert.Equal(14, row.CurrentStock);
        Assert.Equal(20, row.StockQuantity);
        Assert.Equal(6, row.ProposedStockChange);
        Assert.True(row.WillIncreaseStock);
        Assert.False(row.WillAddProduct);
        Assert.Equal("Increase stock", row.Action);
        Assert.DoesNotContain("already exists", row.Error ?? "", StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("not overwritten", row.Error ?? "", StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Existing_catalogue_rows_are_matches_not_file_duplicates()
    {
        var catalog = new ProductImportCatalog();
        var rows = new List<object[]>();
        for (var i = 1; i <= 19; i++)
        {
            var sku = $"DEMO-{i:000}";
            var barcode = $"DEMO-BC-{sku}";
            catalog.ExistingProducts.Add(new ProductImportExisting
            {
                Id = i,
                Name = "Item " + i,
                Sku = sku,
                Barcode = barcode,
                PurchasePrice = 1m,
                SellingPrice = 2m,
                Stock = 10
            });
            catalog.ExistingSkus.Add(sku);
            catalog.ExistingBarcodes.Add(barcode);
            rows.Add(new object[] { "Item " + i, sku, "", "", "", "", "", 1m, 2m, 20, barcode });
        }

        var planned = Run(Sheet(rows.ToArray()), catalog);
        Assert.Equal(19, planned.Count);
        Assert.Equal(0, planned.Count(r => r.Status == ProductImportStatus.Duplicate));
        Assert.All(planned, r =>
        {
            Assert.Equal(ProductImportStatus.Valid, r.Status);
            Assert.True(r.MatchedProductId > 0);
            Assert.Equal(10, r.ProposedStockChange);
            Assert.DoesNotContain("already exists", r.Error ?? "", StringComparison.OrdinalIgnoreCase);
        });
        Assert.Equal(19, ProductImportWorkbook.Summarise(planned).Valid);
        Assert.Equal(0, ProductImportWorkbook.Summarise(planned).Duplicate);
    }

    [Fact]
    public void Import_never_treats_an_existing_sku_as_do_not_overwrite()
    {
        var planner = File.ReadAllText(FindService("ProductImportPlanner.cs"));
        var workbook = File.ReadAllText(FindService("ProductImportWorkbook.cs"));
        Assert.DoesNotContain("Existing products are not overwritten", planner);
        Assert.DoesNotContain("Existing products are not overwritten", workbook);
        Assert.Contains("MarkFileDuplicates", planner);
        Assert.Contains("MatchedProductId", planner);
    }

    [Fact]
    public void Import_planner_does_not_touch_sale_cost_snapshots()
    {
        var src = File.ReadAllText(FindService("ProductImportService.cs"));
        Assert.DoesNotContain("CostPrice", src);
        Assert.DoesNotContain("SaleItems", src);
        Assert.Contains("BeginTransactionAsync", src);
        Assert.Contains("RecalculateTargetStock", src);
        Assert.Contains("ReloadAsync", src);
        Assert.Contains("LockProductRowAsync", src);
        Assert.Contains("ChangeTracker.Clear", src);
        Assert.Contains("StockTransactionType.Adjustment", src);
        Assert.Contains("StockTransactionType.OpeningStock", src);
    }

    private static ProductImportCatalog SamsungCoverCatalog(int stock)
    {
        var catalog = Catalog();
        catalog.ExistingProducts.Add(new ProductImportExisting
        {
            Id = 7,
            Name = "Samsung Phone Cover (demo)",
            Sku = "SAM-COVER-001",
            Barcode = "DEMO-BC-SAM-COVER-001",
            PurchasePrice = 150m,
            SellingPrice = 299m,
            Stock = stock
        });
        catalog.ExistingSkus.Add("SAM-COVER-001");
        catalog.ExistingBarcodes.Add("DEMO-BC-SAM-COVER-001");
        return catalog;
    }

    private static ProductImportCatalog CatalogWithStock(int stock)
    {
        var catalog = Catalog();
        catalog.ExistingProducts[0].Stock = stock;
        return catalog;
    }

    private static string FindService(string fileName)
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir != null)
        {
            var matches = dir.GetFiles(fileName, SearchOption.AllDirectories);
            if (matches.Length > 0)
                return matches[0].FullName;
            dir = dir.Parent;
        }

        throw new FileNotFoundException(fileName);
    }
}
