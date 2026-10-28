using Billing.API.Models;
using Billing.Application.DTOs.Brand;
using Billing.Application.DTOs.Category;
using Billing.Application.DTOs.Expense;
using Billing.Application.DTOs.Purchases;
using Billing.Application.DTOs.Returns;
using Billing.Application.DTOs.Sales;
using Billing.Application.DTOs.Tax;
using Billing.Application.DTOs.Unit;
using Billing.Application.Interfaces;
using Billing.Domain.Enums;
using Billing.Persistence.Context;
using Billing.Shared.Exceptions;
using Microsoft.EntityFrameworkCore;

namespace Billing.API.Services;

/// <summary>
/// Fills clearly labelled DEMO records for a fictional electronics shop.
/// Idempotent: existing DEMO SKUs / mobiles are reused. Never updates or
/// deletes records that are not labelled DEMO. Disabled unless Demo:Enabled
/// is true (and never in Production unless Demo:AllowInProduction is also true).
/// Does not overwrite company tax identity unless Demo:UpdateCompany is true.
/// </summary>
public class DemoDataService
{
    public const string Prefix = "DEMO";
    public const string WalkInMobile = "9000000000";
    public const string WalkInName = "Walk-in Customer";

    /// <summary>Documented test GSTIN (Telangana prefix 36). Not a real registration.</summary>
    public const string TestGstinTelangana = "36AAAAA0000A1Z5";

    /// <summary>Documented test GSTIN (Maharashtra prefix 27). Not a real registration.</summary>
    public const string TestGstinMaharashtra = "27AAAAA0000A1Z5";

    private readonly IHostEnvironment _environment;
    private readonly IConfiguration _configuration;
    private readonly BillingDbContext _context;
    private readonly CategoryService _categories;
    private readonly ProductService _products;
    private readonly CustomerService _customers;
    private readonly SupplierService _suppliers;
    private readonly PurchaseService _purchases;
    private readonly SaleService _sales;
    private readonly SalesReturnService _salesReturns;
    private readonly IExpenseService _expenses;
    private readonly IBrandService _brands;
    private readonly IUnitService _units;
    private readonly ITaxService _taxes;

    public DemoDataService(
        IHostEnvironment environment,
        IConfiguration configuration,
        BillingDbContext context,
        CategoryService categories,
        ProductService products,
        CustomerService customers,
        SupplierService suppliers,
        PurchaseService purchases,
        SaleService sales,
        SalesReturnService salesReturns,
        IExpenseService expenses,
        IBrandService brands,
        IUnitService units,
        ITaxService taxes)
    {
        _environment = environment;
        _configuration = configuration;
        _context = context;
        _categories = categories;
        _products = products;
        _customers = customers;
        _suppliers = suppliers;
        _purchases = purchases;
        _sales = sales;
        _salesReturns = salesReturns;
        _expenses = expenses;
        _brands = brands;
        _units = units;
        _taxes = taxes;
    }

    public void EnsureAllowed()
    {
        var enabled = _configuration.GetValue("Demo:Enabled", false);
        var allowProd = _configuration.GetValue("Demo:AllowInProduction", false);

        if (!enabled)
            throw new BusinessException("Demo data is disabled. Set Demo:Enabled=true in configuration on a development database.");

        if (_environment.IsProduction() && !allowProd)
            throw new BusinessException("Demo data cannot be seeded in Production.");
    }

    public async Task<object> SeedAsync()
    {
        EnsureAllowed();

        var taxId = await EnsureTax();
        var unitId = await EnsureUnit();
        var samsung = await EnsureBrand("DEMO-Samsung");
        var dell = await EnsureBrand("DEMO-Dell");
        var tplink = await EnsureBrand("DEMO-TP-Link");
        var kingston = await EnsureBrand("DEMO-Kingston");
        var generic = await EnsureBrand("DEMO-Generic");

        var mobiles = await EnsureCategory("DEMO - Mobiles");
        var laptops = await EnsureCategory("DEMO - Laptops");
        var accessories = await EnsureCategory("DEMO - Accessories");
        var networking = await EnsureCategory("DEMO - Networking");
        var storage = await EnsureCategory("DEMO - Storage");
        var printers = await EnsureCategory("DEMO - Printers");

        var phone = await EnsureProduct("Demo Android Smartphone", "DEMO-MOB-001", mobiles, samsung, unitId, taxId, 12000, 14999, 15, "8517");
        var laptop = await EnsureProduct("Demo Business Laptop", "DEMO-LAP-001", laptops, dell, unitId, taxId, 42000, 52999, 6, "8471");
        var charger = await EnsureProduct("USB-C Charger", "DEMO-ACC-001", accessories, generic, unitId, taxId, 250, 499, 40, "8504");
        var cable = await EnsureProduct("USB Cable", "DEMO-ACC-002", accessories, generic, unitId, taxId, 80, 149, 50, "8544");
        var mouse = await EnsureProduct("Wireless Mouse", "DEMO-ACC-003", accessories, generic, unitId, taxId, 400, 799, 25, "8471");
        var keyboard = await EnsureProduct("Wireless Keyboard", "DEMO-ACC-004", accessories, generic, unitId, taxId, 700, 1299, 20, "8471");
        var hdmi = await EnsureProduct("HDMI Cable", "DEMO-ACC-005", accessories, generic, unitId, taxId, 150, 299, 30, "8544");
        var card = await EnsureProduct("128 GB Storage Card", "DEMO-STO-001", storage, kingston, unitId, taxId, 450, 799, 18, "8523");
        var ssd = await EnsureProduct("External SSD", "DEMO-STO-002", storage, kingston, unitId, taxId, 3500, 4999, 8, "8471");
        var router = await EnsureProduct("Wi-Fi Router", "DEMO-NET-001", networking, tplink, unitId, taxId, 1800, 2499, 10, "8517");
        var printer = await EnsureProduct("Printer", "DEMO-PRT-001", printers, generic, unitId, taxId, 8000, 9999, 4, "8443");
        await EnsureProduct("Laptop Bag", "DEMO-ACC-006", accessories, generic, unitId, taxId, 600, 999, 12, "4202");
        await EnsureProduct("DEMO Low-stock adapter", "DEMO-LOW-001", accessories, generic, unitId, taxId, 200, 399, 2, "8544");
        await EnsureProduct("DEMO Out of stock toner", "DEMO-OUT-001", printers, generic, unitId, taxId, 1500, 2200, 0, "8443");

        var supplierId = await EnsureSupplier();
        var walkIn = await EnsureCustomer(WalkInName, WalkInMobile, "36", null);
        var north = await EnsureCustomer("DEMO North Retail", "9000000001", "36", TestGstinTelangana);
        var west = await EnsureCustomer("DEMO West Retail", "9000000002", "27", TestGstinMaharashtra);

        if (!await DemoPurchaseExists(cable))
        {
            await _purchases.CreateAsync(new CreatePurchaseRequest
            {
                SupplierId = supplierId,
                PurchaseDate = DateTime.Today.AddDays(-5),
                SupplierInvoiceNumber = "DEMO-BILL-001",
                Remarks = "[DEMO] Restock from wholesale",
                ClientRequestId = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-000000000001"),
                Items = new List<CreatePurchaseItemRequest>
                {
                    new() { ProductId = cable, Quantity = 20, PurchasePrice = 80 },
                    new() { ProductId = charger, Quantity = 10, PurchasePrice = 250 },
                    new() { ProductId = mouse, Quantity = 10, PurchasePrice = 400 },
                    new() { ProductId = hdmi, Quantity = 8, PurchasePrice = 150 },
                    new() { ProductId = router, Quantity = 4, PurchasePrice = 1800 }
                }
            });
        }

        if (!await DemoSaleExists("aaaaaaaa-bbbb-cccc-dddd-000000000011"))
        {
            var created = await _sales.CreateAsync(new CreateSaleRequest
            {
                CustomerId = walkIn,
                Remarks = "[DEMO] Walk-in paid in full",
                ClientRequestId = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-000000000011"),
                Items = new List<CreateSaleItemRequest>
                {
                    new() { ProductId = cable, Quantity = 2 },
                    new() { ProductId = mouse, Quantity = 1 }
                }
            });

            var detail = await _sales.GetByIdAsync(created.SaleId);
            if (detail.Outstanding > 0)
            {
                await _sales.RecordPaymentAsync(created.SaleId, new RecordPaymentRequest
                {
                    Method = PaymentMethod.Cash,
                    Amount = detail.Outstanding,
                    Reference = "DEMO-CASH-WALKIN"
                });
            }

            if (!await DemoReturnExists())
            {
                var line = detail.Items.FirstOrDefault(i => i.ProductId == cable && i.ReturnableQuantity > 0);
                if (line != null)
                {
                    var unitValue = line.LineTotal is > 0
                        ? decimal.Round(line.LineTotal.Value / line.Quantity, 2, MidpointRounding.AwayFromZero)
                        : 0;

                    await _salesReturns.CreateAsync(new CreateSalesReturnRequest
                    {
                        SaleId = created.SaleId,
                        Reason = "[DEMO] One cable returned",
                        RefundAmount = unitValue,
                        RefundMethod = unitValue > 0 ? PaymentMethod.Cash : null,
                        Items = new List<ReturnSaleItemRequest>
                        {
                            new() { SaleItemId = line.Id, Quantity = 1 }
                        }
                    });
                }
            }
        }

        if (!await DemoSaleExists("aaaaaaaa-bbbb-cccc-dddd-000000000012"))
        {
            var created = await _sales.CreateAsync(new CreateSaleRequest
            {
                CustomerId = north,
                Remarks = "[DEMO] Paid in full (UPI)",
                ClientRequestId = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-000000000012"),
                Items = new List<CreateSaleItemRequest>
                {
                    new() { ProductId = phone, Quantity = 1 },
                    new() { ProductId = charger, Quantity = 1 }
                }
            });

            var detail = await _sales.GetByIdAsync(created.SaleId);
            if (detail.Outstanding > 0)
            {
                await _sales.RecordPaymentAsync(created.SaleId, new RecordPaymentRequest
                {
                    Method = PaymentMethod.Upi,
                    Amount = detail.Outstanding,
                    Reference = "DEMO-UPI-NORTH"
                });
            }
        }

        if (!await DemoSaleExists("aaaaaaaa-bbbb-cccc-dddd-000000000013"))
        {
            var created = await _sales.CreateAsync(new CreateSaleRequest
            {
                CustomerId = west,
                Remarks = "[DEMO] Partial payment",
                ClientRequestId = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-000000000013"),
                Items = new List<CreateSaleItemRequest>
                {
                    new() { ProductId = laptop, Quantity = 1 }
                }
            });

            var detail = await _sales.GetByIdAsync(created.SaleId);
            var partial = Math.Min(20000m, detail.Outstanding);
            if (partial > 0 && detail.Outstanding > partial)
            {
                await _sales.RecordPaymentAsync(created.SaleId, new RecordPaymentRequest
                {
                    Method = PaymentMethod.BankTransfer,
                    Amount = partial,
                    Reference = "DEMO-NEFT-WEST"
                });
            }
        }

        if (!await DemoSaleExists("aaaaaaaa-bbbb-cccc-dddd-000000000014"))
        {
            await _sales.CreateAsync(new CreateSaleRequest
            {
                CustomerId = north,
                Remarks = "[DEMO] Unpaid credit sale",
                ClientRequestId = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-000000000014"),
                Items = new List<CreateSaleItemRequest>
                {
                    new() { ProductId = router, Quantity = 1 },
                    new() { ProductId = hdmi, Quantity = 1 }
                }
            });
        }

        if (!await _context.Expenses.AnyAsync(e => !e.IsDeleted && e.Title.StartsWith("DEMO ")))
        {
            await _expenses.CreateAsync(new CreateExpenseDto
            {
                Title = "DEMO shop rent",
                Amount = 15000,
                ExpenseDate = DateTime.Today.AddDays(-8),
                Category = "Rent",
                Remarks = "[DEMO]"
            });
            await _expenses.CreateAsync(new CreateExpenseDto
            {
                Title = "DEMO electricity",
                Amount = 2800,
                ExpenseDate = DateTime.Today.AddDays(-3),
                Category = "Utilities",
                Remarks = "[DEMO]"
            });
        }

        return new
        {
            Success = true,
            Message = "Demo electronics catalogue and sample invoices are in place (labelled DEMO). Re-running this does not duplicate them.",
            Products = new[]
            {
                "DEMO-MOB-001", "DEMO-LAP-001", "DEMO-ACC-001", "DEMO-ACC-002",
                "DEMO-ACC-003", "DEMO-ACC-004", "DEMO-ACC-005", "DEMO-ACC-006",
                "DEMO-STO-001", "DEMO-STO-002", "DEMO-NET-001", "DEMO-PRT-001",
                "DEMO-LOW-001", "DEMO-OUT-001"
            },
            Note = "Use a development copy of the database. Test GSTINs 36AAAAA0000A1Z5 / 27AAAAA0000A1Z5 are fictional. Demo invoices stay in reports until you use a fresh copy or an approved reset."
        };
    }

    private async Task<int> EnsureTax()
    {
        var existing = await _context.Taxes.AsNoTracking()
            .Where(t => !t.IsDeleted && t.IsActive && t.Percentage == 18)
            .OrderBy(t => t.Name.StartsWith("DEMO") ? 0 : 1)
            .Select(t => t.Id)
            .FirstOrDefaultAsync();
        if (existing != 0) return existing;

        var created = await _taxes.CreateAsync(new SaveTaxRequest
        {
            Name = "DEMO GST 18%",
            Percentage = 18,
            Description = "Demo tax rate",
            IsActive = true
        });
        return created.Id;
    }

    private async Task<int> EnsureUnit()
    {
        var existing = await _context.Units.AsNoTracking()
            .Where(u => !u.IsDeleted && u.IsActive)
            .OrderBy(u => u.Name.Contains("Piece") || u.ShortName == "Pcs" ? 0 : 1)
            .Select(u => u.Id)
            .FirstOrDefaultAsync();
        if (existing != 0) return existing;

        var created = await _units.CreateAsync(new SaveUnitRequest
        {
            Name = "Pieces",
            ShortName = "Pcs",
            IsActive = true
        });
        return created.Id;
    }

    private async Task<int> EnsureBrand(string name)
    {
        var existing = await _context.Brands.AsNoTracking()
            .Where(b => !b.IsDeleted && b.Name == name)
            .Select(b => b.Id)
            .FirstOrDefaultAsync();
        if (existing != 0) return existing;

        var created = await _brands.CreateAsync(new SaveBrandRequest
        {
            Name = name,
            Description = "Demo brand",
            IsActive = true
        });
        return created.Id;
    }

    private async Task<int> EnsureCategory(string name)
    {
        var existing = await _context.Categories.AsNoTracking()
            .Where(c => !c.IsDeleted && c.Name == name)
            .Select(c => c.Id)
            .FirstOrDefaultAsync();
        if (existing != 0) return existing;

        var created = await _categories.CreateAsync(new SaveCategoryRequest
        {
            Name = name,
            Description = "Demo category",
            IsActive = true
        });
        return created.Id;
    }

    private async Task<int> EnsureProduct(
        string name, string sku, int categoryId, int brandId, int unitId, int? taxId,
        decimal cost, decimal price, int stock, string hsn)
    {
        var existing = await _context.Products.AsNoTracking()
            .Where(p => !p.IsDeleted && p.SKU == sku)
            .Select(p => p.Id)
            .FirstOrDefaultAsync();
        if (existing != 0) return existing;

        return await _products.CreateAsync(new ProductDto
        {
            Name = name,
            SKU = sku,
            PurchasePrice = cost,
            SellingPrice = price,
            Stock = stock,
            CategoryId = categoryId,
            BrandId = brandId,
            UnitId = unitId,
            TaxId = taxId,
            HsnCode = hsn
        });
    }

    private async Task<int> EnsureCustomer(string name, string mobile, string state, string? gstin)
    {
        var existing = await _context.Customers.AsNoTracking()
            .Where(c => !c.IsDeleted && c.Mobile == mobile)
            .Select(c => c.Id)
            .FirstOrDefaultAsync();
        if (existing != 0) return existing;

        return await _customers.CreateAsync(new CustomerDto
        {
            Name = name,
            Mobile = mobile,
            StateCode = state,
            Gstin = gstin,
            Address = "Demo address — fictional shop"
        });
    }

    private async Task<int> EnsureSupplier()
    {
        var existing = await _context.Suppliers.AsNoTracking()
            .Where(s => !s.IsDeleted && s.Mobile == "9000000099")
            .Select(s => s.Id)
            .FirstOrDefaultAsync();
        if (existing != 0) return existing;

        return await _suppliers.CreateAsync(new SupplierDto
        {
            Name = "DEMO wholesale contact",
            CompanyName = "DEMO Wholesale Supplies",
            Mobile = "9000000099",
            StateCode = "36",
            Gstin = TestGstinTelangana,
            Address = "Demo warehouse — fictional"
        });
    }

    private Task<bool> DemoPurchaseExists(int productId) =>
        _context.PurchaseItems.AsNoTracking()
            .AnyAsync(i => i.ProductId == productId && i.Purchase.Remarks != null && i.Purchase.Remarks.Contains("[DEMO]"));

    private Task<bool> DemoSaleExists(string clientRequestId) =>
        _context.Sales.AsNoTracking()
            .AnyAsync(s => s.ClientRequestId == Guid.Parse(clientRequestId));

    private Task<bool> DemoReturnExists() =>
        _context.SalesReturns.AsNoTracking()
            .AnyAsync(r => r.Reason != null && r.Reason.Contains("[DEMO]"));
}
