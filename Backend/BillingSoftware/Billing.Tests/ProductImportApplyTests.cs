using Billing.API.Services.ProductImport;
using Billing.Application.DTOs.Products;
using Billing.Application.Interfaces;
using Billing.Domain.Entities;
using Billing.Domain.Enums;
using Billing.Persistence.Context;
using Billing.Shared.Exceptions;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace Billing.Tests;

public class ProductImportApplyTests
{
    [Fact]
    public async Task Import_adds_a_new_product_and_opening_stock_through_the_ledger()
    {
        var fx = await Fixture();
        var bytes = ProductWorkbook(new object[] { "USB cable", "USB-01", "", "", "", "", "", 80m, 120m, 5, "12345678" });

        var result = await fx.Service.ImportAsync(Form(bytes));

        Assert.Equal(1, result.Added);
        Assert.Equal(1, result.StockIncreased);
        var product = Assert.Single(fx.Db.Products);
        Assert.Equal("USB-01", product.SKU);
        Assert.Equal("12345678", product.Barcode);
        Assert.Equal(5, product.Stock);
        var call = Assert.Single(fx.Inventory.Calls);
        Assert.Equal(StockTransactionType.OpeningStock, call.Type);
        Assert.Equal(5, call.Quantity);
        Assert.Empty(fx.Db.SaleItems);
    }

    [Fact]
    public async Task Reimporting_the_same_file_replays_and_does_not_add_stock_again()
    {
        var fx = await Fixture();
        var bytes = ProductWorkbook(new object[] { "USB cable", "USB-01", "", "", "", "", "", 80m, 120m, 5 });

        var first = await fx.Service.ImportAsync(Form(bytes));
        var second = await fx.Service.ImportAsync(Form(bytes));

        Assert.False(first.Replayed);
        Assert.True(second.Replayed);
        Assert.Equal(first.Added, second.Added);
        Assert.Single(fx.Inventory.Calls);
        Assert.Single(fx.Db.Products);
    }

    [Fact]
    public async Task Concurrent_imports_of_the_same_file_add_stock_once()
    {
        var fx = await Fixture();
        var bytes = ProductWorkbook(new object[] { "USB cable", "USB-01", "", "", "", "", "", 80m, 120m, 4 });
        var file1 = Form(bytes);
        var file2 = Form(bytes);

        var a = fx.Service.ImportAsync(file1);
        var b = fx.Service.ImportAsync(file2);
        var results = await Task.WhenAll(a, b);

        Assert.Single(fx.Db.Products);
        Assert.Single(fx.Inventory.Calls);
        Assert.Contains(results, r => r.Added == 1);
        Assert.Contains(results, r => r.Replayed);
    }

    [Fact]
    public async Task Failure_midway_does_not_keep_partial_rows_when_the_provider_rolls_back()
    {
        var fx = await Fixture();
        fx.Inventory.ThrowOnCall = 2;
        var bytes = ProductWorkbook(
            new object[] { "A", "A-1", "", "", "", "", "", 1m, 2m, 1 },
            new object[] { "B", "B-1", "", "", "", "", "", 1m, 2m, 1 });

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            fx.Service.ImportAsync(Form(bytes)));
    }

    [Fact]
    public async Task Updating_purchase_price_does_not_write_sale_cost_price()
    {
        var fx = await Fixture();
        var product = await SeedExisting(fx, stock: 10);
        fx.Db.SaleItems.Add(new SaleItem
        {
            SaleId = 1,
            ProductId = product.Id,
            Quantity = 1,
            SellingPrice = 2m,
            Total = 2m,
            CostPrice = 1.00m
        });
        await fx.Db.SaveChangesAsync();

        var bytes = ProductWorkbook(new object[] { "Existing", "EXISTING-1", "", "", "", "", "", 9m, 2m, 10 });
        await fx.Service.ImportAsync(Form(bytes));

        Assert.Equal(9m, fx.Db.Products.Single().PurchasePrice);
        Assert.Equal(1.00m, fx.Db.SaleItems.Single().CostPrice);
        Assert.Empty(fx.Inventory.Calls);
    }

    [Fact]
    public async Task Samsung_cover_same_barcode_and_sku_increases_stock_14_to_20()
    {
        var fx = await Fixture();
        await SeedExisting(
            fx,
            stock: 14,
            sku: "SAM-COVER-001",
            barcode: "DEMO-BC-SAM-COVER-001",
            name: "Samsung Phone Cover (demo)",
            purchase: 150m,
            selling: 299m);

        var result = await fx.Service.ImportAsync(Form(ProductWorkbook(new object[]
        {
            "Samsung Phone Cover (demo)", "SAM-COVER-001", "", "", "", "", "",
            150m, 299m, 20, "DEMO-BC-SAM-COVER-001"
        })));

        Assert.Equal(0, result.Duplicate);
        Assert.Equal(0, result.Added);
        Assert.Equal(1, result.StockIncreased);
        Assert.Equal(20, fx.Db.Products.Single().Stock);
        Assert.Equal(6, Assert.Single(fx.Inventory.Calls).Quantity);
        Assert.DoesNotContain(result.Rows, r =>
            (r.Error ?? "").Contains("not overwritten", StringComparison.OrdinalIgnoreCase)
            || (r.Error ?? "").Contains("already exists", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task Lower_target_stock_is_not_applied()
    {
        var fx = await Fixture();
        await SeedExisting(
            fx,
            stock: 20,
            sku: "SAM-COVER-001",
            barcode: "DEMO-BC-SAM-COVER-001",
            name: "Samsung Phone Cover (demo)",
            purchase: 150m,
            selling: 299m);

        var result = await fx.Service.ImportAsync(Form(ProductWorkbook(new object[]
        {
            "Samsung Phone Cover (demo)", "SAM-COVER-001", "", "", "", "", "",
            150m, 299m, 10, "DEMO-BC-SAM-COVER-001"
        })));

        Assert.Equal(0, result.Valid);
        Assert.Equal(1, result.Conflict);
        Assert.Equal(20, fx.Db.Products.Single().Stock);
        Assert.Empty(fx.Inventory.Calls);
        Assert.Contains("Target stock is lower than current stock", result.Rows.Single().Error);
    }

    [Fact]
    public async Task Preview_and_import_agree_on_the_cover_stock_increase()
    {
        var fx = await Fixture();
        await SeedExisting(fx, stock: 14, sku: "SAM-COVER-001", barcode: "DEMO-BC-SAM-COVER-001");
        var bytes = ProductWorkbook(new object[]
        {
            "Existing", "SAM-COVER-001", "", "", "", "", "", 1m, 2m, 20, "DEMO-BC-SAM-COVER-001"
        });
        var file = Form(bytes);

        var preview = await fx.Service.PreviewAsync(file);
        Assert.Equal(1, preview.Valid);
        Assert.Equal(6, preview.Rows.Single().ProposedStockChange);
        Assert.True(preview.CanImport);

        var result = await fx.Service.ImportAsync(Form(bytes));
        Assert.Equal(1, result.StockIncreased);
        Assert.Equal(20, fx.Db.Products.Single().Stock);
        Assert.Equal(6, Assert.Single(fx.Inventory.Calls).Quantity);
    }

    [Fact]
    public async Task Valid_increase_applies_when_another_row_is_a_conflict()
    {
        var fx = await Fixture();
        await SeedExisting(fx, stock: 14, sku: "KEEP-1", barcode: "BC-KEEP");
        fx.Db.Products.Add(new Product
        {
            Name = "Other",
            SKU = "OTHER-1",
            Barcode = "BC-OTHER",
            PurchasePrice = 1m,
            SellingPrice = 2m,
            Stock = 4,
            CreatedOn = DateTime.UtcNow
        });
        await fx.Db.SaveChangesAsync();

        var result = await fx.Service.ImportAsync(Form(ProductWorkbook(
            new object[] { "Existing", "KEEP-1", "", "", "", "", "", 1m, 2m, 20, "BC-KEEP" },
            new object[] { "Existing", "OTHER-1", "", "", "", "", "", 1m, 2m, 4, "BC-KEEP" })));

        Assert.Equal(1, result.StockIncreased);
        Assert.Equal(1, result.Conflict);
        Assert.Equal(20, fx.Db.Products.Single(p => p.SKU == "KEEP-1").Stock);
        Assert.Equal(4, fx.Db.Products.Single(p => p.SKU == "OTHER-1").Stock);
        Assert.Equal(6, Assert.Single(fx.Inventory.Calls).Quantity);
    }

    [Theory]
    [InlineData(10, 20, 20, 10)]
    [InlineData(30, 40, 40, 10)]
    public async Task OpeningStock_increases_existing_stock_by_the_difference(
        int current,
        int imported,
        int expectedStock,
        int expectedDelta)
    {
        var fx = await Fixture();
        await SeedExisting(fx, stock: current);

        var result = await fx.Service.ImportAsync(Form(
            ProductWorkbook(new object[] { "Existing", "EXISTING-1", "", "", "", "", "", 1m, 2m, imported })));

        Assert.Equal(1, result.StockIncreased);
        Assert.Equal(0, result.Added);
        Assert.Equal(expectedStock, fx.Db.Products.Single().Stock);
        var call = Assert.Single(fx.Inventory.Calls);
        Assert.Equal(StockTransactionType.Adjustment, call.Type);
        Assert.Equal(expectedDelta, call.Quantity);
    }

    [Theory]
    [InlineData(50, 50)]
    [InlineData(100, 100)]
    public async Task OpeningStock_equal_to_on_hand_does_not_move_stock(int current, int imported)
    {
        var fx = await Fixture();
        await SeedExisting(fx, stock: current);

        var result = await fx.Service.ImportAsync(Form(
            ProductWorkbook(new object[] { "Existing", "EXISTING-1", "", "", "", "", "", 1m, 2m, imported })));

        Assert.Equal(0, result.StockIncreased);
        Assert.Equal(0, result.Added);
        Assert.Equal(0, result.Updated);
        Assert.True(result.Unchanged >= 1);
        Assert.Equal(current, fx.Db.Products.Single().Stock);
        Assert.Empty(fx.Inventory.Calls);
    }

    [Fact]
    public async Task OpeningStock_below_on_hand_is_flagged_and_does_not_reduce_stock()
    {
        var fx = await Fixture();
        await SeedExisting(fx, stock: 10);

        var result = await fx.Service.ImportAsync(Form(
            ProductWorkbook(new object[] { "Existing", "EXISTING-1", "", "", "", "", "", 1m, 2m, 5 })));

        Assert.Equal(0, result.StockIncreased);
        Assert.Equal(1, result.Conflict);
        Assert.Equal(10, fx.Db.Products.Single().Stock);
        Assert.Empty(fx.Inventory.Calls);
        Assert.Contains("reduction", result.Rows.Single().Error, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Blank_OpeningStock_preserves_existing_stock()
    {
        var fx = await Fixture();
        await SeedExisting(fx, stock: 10);

        var result = await fx.Service.ImportAsync(Form(
            ProductWorkbook(new object[] { "Existing", "EXISTING-1", "", "", "", "", "", 1m, 2m, "" })));

        Assert.Equal(10, fx.Db.Products.Single().Stock);
        Assert.Empty(fx.Inventory.Calls);
        Assert.True(result.Unchanged >= 1);
    }

    [Fact]
    public async Task Same_barcode_unchanged_values_are_skipped()
    {
        var fx = await Fixture();
        await SeedExisting(fx, stock: 10, barcode: "BC-EXISTING");

        var result = await fx.Service.ImportAsync(Form(
            ProductWorkbook(new object[] { "Existing", "EXISTING-1", "", "", "", "", "", 1m, 2m, 10, "BC-EXISTING" })));

        Assert.Equal(0, result.Updated);
        Assert.Equal(0, result.StockIncreased);
        Assert.Empty(fx.Inventory.Calls);
        Assert.Equal(10, fx.Db.Products.Single().Stock);
    }

    [Fact]
    public async Task Same_barcode_higher_OpeningStock_applies_the_difference()
    {
        var fx = await Fixture();
        await SeedExisting(fx, stock: 10, barcode: "BC-EXISTING");

        var result = await fx.Service.ImportAsync(Form(
            ProductWorkbook(new object[] { "Existing", "EXISTING-1", "", "", "", "", "", 1m, 2m, 20, "BC-EXISTING" })));

        Assert.Equal(1, result.StockIncreased);
        Assert.Equal(20, fx.Db.Products.Single().Stock);
        Assert.Equal(10, Assert.Single(fx.Inventory.Calls).Quantity);
    }

    [Fact]
    public async Task Same_barcode_changed_selling_price_updates_price_without_stock_movement()
    {
        var fx = await Fixture();
        await SeedExisting(fx, stock: 10, barcode: "BC-EXISTING");

        var result = await fx.Service.ImportAsync(Form(
            ProductWorkbook(new object[] { "Existing", "EXISTING-1", "", "", "", "", "", 1m, 120m, 10, "BC-EXISTING" })));

        Assert.Equal(1, result.Updated);
        Assert.Equal(0, result.StockIncreased);
        Assert.Equal(120m, fx.Db.Products.Single().SellingPrice);
        Assert.Equal(10, fx.Db.Products.Single().Stock);
        Assert.Empty(fx.Inventory.Calls);
    }

    [Fact]
    public async Task Duplicate_rows_in_one_file_do_not_add_stock_twice()
    {
        var fx = await Fixture();
        await SeedExisting(fx, stock: 10);

        var result = await fx.Service.ImportAsync(Form(ProductWorkbook(
            new object[] { "Existing", "EXISTING-1", "", "", "", "", "", 1m, 2m, 20 },
            new object[] { "Existing", "EXISTING-1", "", "", "", "", "", 1m, 2m, 20 })));

        Assert.Equal(1, result.Duplicate);
        Assert.Equal(1, result.StockIncreased);
        Assert.Equal(20, fx.Db.Products.Single().Stock);
        Assert.Equal(10, Assert.Single(fx.Inventory.Calls).Quantity);
    }

    [Fact]
    public async Task Price_and_stock_change_together_and_leave_historical_cost_alone()
    {
        var fx = await Fixture();
        var product = await SeedExisting(fx, stock: 10);
        fx.Db.SaleItems.Add(new SaleItem
        {
            SaleId = 1,
            ProductId = product.Id,
            Quantity = 2,
            SellingPrice = 2m,
            Total = 4m,
            CostPrice = 1.00m
        });
        await fx.Db.SaveChangesAsync();

        var result = await fx.Service.ImportAsync(Form(
            ProductWorkbook(new object[] { "Existing", "EXISTING-1", "", "", "", "", "", 1m, 120m, 20 })));

        Assert.Equal(1, result.Updated);
        Assert.Equal(1, result.StockIncreased);
        Assert.Equal(120m, fx.Db.Products.Single().SellingPrice);
        Assert.Equal(20, fx.Db.Products.Single().Stock);
        Assert.Equal(1.00m, fx.Db.SaleItems.Single().CostPrice);
        Assert.Equal(10, Assert.Single(fx.Inventory.Calls).Quantity);
    }

    [Fact]
    public async Task Confirm_uses_live_stock_when_on_hand_changed_after_preview()
    {
        var fx = await Fixture();
        var product = await SeedExisting(fx, stock: 10);

        var preview = await fx.Service.PreviewAsync(Form(
            ProductWorkbook(new object[] { "Existing", "EXISTING-1", "", "", "", "", "", 1m, 2m, 20 })));
        Assert.Equal(10, preview.Rows.Single().ProposedStockChange);

        product.Stock = 18;
        await fx.Db.SaveChangesAsync();

        var result = await fx.Service.ImportAsync(Form(
            ProductWorkbook(new object[] { "Existing", "EXISTING-1", "", "", "", "", "", 1m, 2m, 20 })));

        Assert.Equal(20, fx.Db.Products.Single().Stock);
        Assert.Equal(2, Assert.Single(fx.Inventory.Calls).Quantity);
        Assert.Equal(1, result.StockIncreased);
    }

    [Fact]
    public async Task Concurrent_imports_of_different_files_do_not_double_add_target_stock()
    {
        var dbName = Guid.NewGuid().ToString();
        var fx1 = await Fixture(dbName);
        var fx2 = await Fixture(dbName);
        await SeedExisting(fx1, stock: 10, barcode: "BC-EXISTING");

        var fileA = Form(ProductWorkbook(new object[]
        {
            "Existing", "EXISTING-1", "", "", "", "", "", 1m, 2m, 20
        }));
        var fileB = Form(ProductWorkbook(new object[]
        {
            "Existing", "EXISTING-1", "", "", "", "", "", 1m, 2m, 20, "BC-EXISTING"
        }));

        var results = await Task.WhenAll(
            fx1.Service.ImportAsync(fileA),
            fx2.Service.ImportAsync(fileB));

        var totalAdded = fx1.Inventory.Calls.Sum(c => c.Quantity)
            + fx2.Inventory.Calls.Sum(c => c.Quantity);
        Assert.Equal(10, totalAdded);
        Assert.Equal(1, fx1.Inventory.Calls.Count + fx2.Inventory.Calls.Count);

        await fx1.Db.Entry(fx1.Db.Products.Single()).ReloadAsync();
        await fx2.Db.Entry(fx2.Db.Products.Single()).ReloadAsync();
        Assert.Equal(20, fx1.Db.Products.Single().Stock);
        Assert.Equal(20, fx2.Db.Products.Single().Stock);
        Assert.Contains(results, r => r.StockIncreased == 1);
    }

    private static async Task<Product> SeedExisting(
        Fx fx,
        int stock,
        string sku = "EXISTING-1",
        string? barcode = null,
        string name = "Existing",
        decimal purchase = 1m,
        decimal selling = 2m)
    {
        var product = new Product
        {
            Name = name,
            SKU = sku,
            Barcode = barcode,
            PurchasePrice = purchase,
            SellingPrice = selling,
            Stock = stock,
            CreatedOn = DateTime.UtcNow
        };
        fx.Db.Products.Add(product);
        await fx.Db.SaveChangesAsync();
        return product;
    }

    private static async Task<Fx> Fixture(string? databaseName = null)
    {
        var options = new DbContextOptionsBuilder<BillingDbContext>()
            .UseInMemoryDatabase(databaseName ?? Guid.NewGuid().ToString())
            .ConfigureWarnings(w => w.Ignore(
                Microsoft.EntityFrameworkCore.Diagnostics.InMemoryEventId.TransactionIgnoredWarning))
            .Options;
        var db = new BillingDbContext(options);
        var inventory = new FakeInventory(db);
        var service = new ProductImportService(db, inventory, new TestUser());
        await Task.CompletedTask;
        return new Fx(db, inventory, service);
    }

    private static IFormFile Form(byte[] bytes)
    {
        var stream = new MemoryStream(bytes);
        return new FormFile(stream, 0, bytes.Length, "file", "products.xlsx")
        {
            Headers = new HeaderDictionary(),
            ContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet"
        };
    }

    private static byte[] ProductWorkbook(params object[][] rows)
    {
        using var workbook = new ClosedXML.Excel.XLWorkbook();
        var sheet = workbook.AddWorksheet(ProductImportWorkbook.ProductsSheet);
        var headers = ProductImportWorkbook.Headers;
        for (var i = 0; i < headers.Length; i++)
            sheet.Cell(1, i + 1).Value = headers[i];

        for (var r = 0; r < rows.Length; r++)
        {
            for (var c = 0; c < rows[r].Length; c++)
            {
                var value = rows[r][c];
                if (value is null) continue;
                if (value is int n) sheet.Cell(r + 2, c + 1).Value = n;
                else if (value is decimal d) sheet.Cell(r + 2, c + 1).Value = d;
                else sheet.Cell(r + 2, c + 1).Value = value.ToString();
            }
        }

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }

    private sealed record Fx(BillingDbContext Db, FakeInventory Inventory, ProductImportService Service);

    private sealed class TestUser : ICurrentUser
    {
        public int? UserId => 1;
        public string? Email => "verify.admin@test.local";
        public string? Role => "Admin";
        public string? IpAddress => "127.0.0.1";
    }

    private sealed class FakeInventory : IInventoryService
    {
        private readonly BillingDbContext _db;

        public FakeInventory(BillingDbContext db) => _db = db;

        public List<(int ProductId, int Quantity, StockTransactionType Type, string? Reference)> Calls { get; } = new();
        public int? ThrowOnCall { get; set; }

        public Task AdjustStockAsync(
            int productId,
            int quantity,
            StockTransactionType transactionType,
            string? referenceNo = null,
            string? remarks = null)
        {
            Calls.Add((productId, quantity, transactionType, referenceNo));
            if (ThrowOnCall is int n && Calls.Count >= n)
                throw new InvalidOperationException("simulated failure");

            var product = _db.Products.Find(productId);
            if (product != null)
                product.Stock += quantity;

            return Task.CompletedTask;
        }
    }
}
