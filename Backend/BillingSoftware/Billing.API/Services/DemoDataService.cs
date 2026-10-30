using Billing.API.Models;
using Billing.Application.DTOs.Brand;
using Billing.Application.DTOs.Category;
using Billing.Application.DTOs.Expense;
using Billing.Application.DTOs.Purchases;
using Billing.Application.DTOs.Returns;
using Billing.Application.DTOs.Sales;
using Billing.Application.DTOs.Warranty;
using Billing.Application.Warranty;
using Billing.Application.DTOs.Tax;
using Billing.Application.DTOs.Unit;
using Billing.Application.Interfaces;
using Billing.Domain.Enums;
using Billing.Persistence.Context;
using Billing.Shared.Exceptions;
using Microsoft.EntityFrameworkCore;

namespace Billing.API.Services;

/// <summary>
/// Fills sample grocery + Samsung catalogue for a fictional demo shop.
/// Prices, barcodes and invoices are labelled demo/sample — not real market
/// data or manufacturer EANs. Idempotent: existing SKUs / mobiles are reused.
/// Disabled unless Demo:Enabled is true (and never in Production unless
/// Demo:AllowInProduction is also true).
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
    private readonly IWarrantyService _warranties;

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
        ITaxService taxes,
        IWarrantyService warranties)
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
        _warranties = warranties;
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

    public static string DemoBarcode(string sku) => $"DEMO-BC-{sku}";

    public async Task<object> SeedAsync()
    {
        EnsureAllowed();

        var gst5 = await EnsureTax(5m, "GST 5%");
        var gst18 = await EnsureTax(18m, "GST 18%");

        var piece = await EnsureUnit("Piece", "Pcs");
        var packet = await EnsureUnit("Packet", "Pkt");
        await EnsureUnit("Kilogram", "Kg");
        var litre = await EnsureUnit("Litre", "L");

        var samsung = await EnsureBrand("Samsung");
        var aashirvaad = await EnsureBrand("Aashirvaad");
        var tata = await EnsureBrand("Tata");
        var fortune = await EnsureBrand("Fortune");
        var parle = await EnsureBrand("Parle");
        var britannia = await EnsureBrand("Britannia");
        var amul = await EnsureBrand("Amul");

        var grocery = await EnsureCategory("Grocery");
        var packaged = await EnsureCategory("Packaged Food");
        var beverages = await EnsureCategory("Beverages");
        var ricePulses = await EnsureCategory("Rice & Pulses");
        var mobiles = await EnsureCategory("Mobile Phones");
        var accessories = await EnsureCategory("Mobile Accessories");

        var rice = await EnsureProduct("Rice 1 kg (demo)", "GRC-RICE-001", ricePulses, tata, packet, gst5, 55, 70, 100, "1006", DemoBarcode("GRC-RICE-001"));
        await EnsureProduct("Toor Dal 1 kg (demo)", "GRC-DAL-001", ricePulses, tata, packet, gst5, 100, 125, 60, "0713", DemoBarcode("GRC-DAL-001"));
        await EnsureProduct("Sugar 1 kg (demo)", "GRC-SUGAR-001", grocery, tata, packet, gst5, 38, 45, 80, "1701", DemoBarcode("GRC-SUGAR-001"));
        await EnsureProduct("Atta 5 kg (demo)", "GRC-ATTA-005", grocery, aashirvaad, packet, gst5, 180, 220, 30, "1101", DemoBarcode("GRC-ATTA-005"));
        await EnsureProduct("Sunflower Oil 1 L (demo)", "GRC-OIL-001", grocery, fortune, litre, gst5, 115, 140, 50, "1512", DemoBarcode("GRC-OIL-001"));
        await EnsureProduct("Salt 1 kg (demo)", "GRC-SALT-001", grocery, tata, packet, gst5, 18, 22, 100, "2501", DemoBarcode("GRC-SALT-001"));
        var biscuit = await EnsureProduct("Parle-G Biscuit Pack (demo)", "GRC-BISC-001", packaged, parle, packet, gst5, 8, 10, 100, "1905", DemoBarcode("GRC-BISC-001"));
        await EnsureProduct("Britannia Bread (demo)", "GRC-BREAD-001", packaged, britannia, packet, gst5, 30, 35, 40, "1905", DemoBarcode("GRC-BREAD-001"));
        await EnsureProduct("Amul Butter (demo)", "GRC-BUTTER-001", packaged, amul, packet, gst5, 50, 60, 40, "0405", DemoBarcode("GRC-BUTTER-001"));
        await EnsureProduct("Mineral Water 1 L (demo)", "GRC-WATER-001", beverages, amul, litre, gst5, 12, 20, 80, "2201", DemoBarcode("GRC-WATER-001"));

        var a06 = await EnsureProduct("Samsung Galaxy A06 4GB/64GB (demo)", "SAM-A06-464", mobiles, samsung, piece, gst18, 7000, 7999, 5, "8517",
            DemoBarcode("SAM-A06-464"), warrantyValue: 1, warrantyUnit: "Years", serial: true);
        await EnsureProduct("Samsung Galaxy A16 5G 8GB/128GB (demo)", "SAM-A16-8128", mobiles, samsung, piece, gst18, 16000, 18999, 5, "8517",
            DemoBarcode("SAM-A16-8128"), warrantyValue: 1, warrantyUnit: "Years", serial: true);
        await EnsureProduct("Samsung Galaxy A26 5G 8GB/128GB (demo)", "SAM-A26-8128", mobiles, samsung, piece, gst18, 22000, 24999, 3, "8517",
            DemoBarcode("SAM-A26-8128"), warrantyValue: 1, warrantyUnit: "Years", serial: true);
        await EnsureProduct("Samsung Galaxy M35 5G 8GB/128GB (demo)", "SAM-M35-8128", mobiles, samsung, piece, gst18, 17000, 19999, 4, "8517",
            DemoBarcode("SAM-M35-8128"), warrantyValue: 1, warrantyUnit: "Years", serial: true);
        await EnsureProduct("Samsung Galaxy S24 FE 8GB/128GB (demo)", "SAM-S24FE-8128", mobiles, samsung, piece, gst18, 35000, 39999, 2, "8517",
            DemoBarcode("SAM-S24FE-8128"), warrantyValue: 1, warrantyUnit: "Years", serial: true);

        var charger = await EnsureProduct("Samsung USB-C Charger (demo)", "SAM-CHARGER-001", accessories, samsung, piece, gst18, 500, 799, 10, "8504",
            DemoBarcode("SAM-CHARGER-001"), warrantyValue: 6, warrantyUnit: "Months");
        await EnsureProduct("USB-C Cable (demo)", "SAM-CABLE-001", accessories, samsung, piece, gst18, 100, 199, 20, "8544",
            DemoBarcode("SAM-CABLE-001"));
        var cover = await EnsureProduct("Samsung Phone Cover (demo)", "SAM-COVER-001", accessories, samsung, piece, gst18, 150, 299, 15, "3926",
            DemoBarcode("SAM-COVER-001"));
        await EnsureProduct("Tempered Glass (demo)", "SAM-GLASS-001", accessories, samsung, piece, gst18, 50, 149, 20, "7007",
            DemoBarcode("SAM-GLASS-001"));

        await EnsureSupplier();
        var walkIn = await EnsureCustomer(WalkInName, WalkInMobile, "36", null);
        var north = await EnsureCustomer("DEMO North Retail", "9000000001", "36", TestGstinTelangana);
        var west = await EnsureCustomer("DEMO West Retail", "9000000002", "27", TestGstinMaharashtra);

        if (!await DemoSaleExists("aaaaaaaa-bbbb-cccc-dddd-000000000021"))
        {
            var created = await _sales.CreateAsync(new CreateSaleRequest
            {
                CustomerId = walkIn,
                Remarks = "[DEMO] Grocery walk-in barcode checkout",
                ClientRequestId = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-000000000021"),
                Items = new List<CreateSaleItemRequest>
                {
                    new() { ProductId = rice, Quantity = 2, RegisterWarranty = false },
                    new() { ProductId = biscuit, Quantity = 2, RegisterWarranty = false }
                }
            });

            var detail = await _sales.GetByIdAsync(created.SaleId);
            if (detail.Outstanding > 0)
            {
                await _sales.RecordPaymentAsync(created.SaleId, new RecordPaymentRequest
                {
                    Method = PaymentMethod.Cash,
                    Amount = detail.Outstanding,
                    Reference = "DEMO-CASH-GROCERY"
                });
            }

            if (!await DemoReturnExists())
            {
                var line = detail.Items.FirstOrDefault(i => i.ProductId == biscuit && i.ReturnableQuantity > 0);
                if (line != null)
                {
                    var unitValue = line.LineTotal is > 0
                        ? decimal.Round(line.LineTotal.Value / line.Quantity, 2, MidpointRounding.AwayFromZero)
                        : 0;

                    await _salesReturns.CreateAsync(new CreateSalesReturnRequest
                    {
                        SaleId = created.SaleId,
                        Reason = "[DEMO] One biscuit pack returned",
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

        if (!await DemoSaleExists("aaaaaaaa-bbbb-cccc-dddd-000000000022"))
        {
            var created = await _sales.CreateAsync(new CreateSaleRequest
            {
                CustomerId = north,
                Remarks = "[DEMO] Samsung phone paid in full (UPI)",
                ClientRequestId = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-000000000022"),
                Items = new List<CreateSaleItemRequest>
                {
                    new()
                    {
                        ProductId = a06,
                        Quantity = 1,
                        RegisterWarranty = true,
                        WarrantyDurationValue = 1,
                        WarrantyDurationUnit = "Years",
                        SerialNumbers = new List<string> { "DEMO-SN-A06-001" }
                    },
                    new()
                    {
                        ProductId = charger,
                        Quantity = 1,
                        RegisterWarranty = true,
                        WarrantyDurationValue = 6,
                        WarrantyDurationUnit = "Months"
                    }
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

        if (!await DemoSaleExists("aaaaaaaa-bbbb-cccc-dddd-000000000023"))
        {
            var created = await _sales.CreateAsync(new CreateSaleRequest
            {
                CustomerId = west,
                Remarks = "[DEMO] Partial payment on cover — profit ignores payments",
                ClientRequestId = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-000000000023"),
                Items = new List<CreateSaleItemRequest>
                {
                    new() { ProductId = cover, Quantity = 1, RegisterWarranty = false }
                }
            });

            var detail = await _sales.GetByIdAsync(created.SaleId);
            var partial = Math.Min(100m, detail.Outstanding);
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

        if (!await _context.WarrantyClaims.AnyAsync(c => c.ReportedProblem.Contains("[DEMO]")))
        {
            var chargerWarranty = await _context.Warranties
                .Where(w => w.ProductName.Contains("USB-C Charger") && w.InvoiceNumber != "")
                .OrderByDescending(w => w.Id)
                .FirstOrDefaultAsync();

            if (chargerWarranty != null &&
                WarrantyDates.IsEligible(chargerWarranty.StartDate, chargerWarranty.EndDate, DateTime.Today, chargerWarranty.IsVoided)
                && !await _context.WarrantyClaims.AnyAsync(c => c.WarrantyId == chargerWarranty.Id))
            {
                var submitted = await _warranties.SubmitClaimAsync(chargerWarranty.Id, new SubmitWarrantyClaimRequest
                {
                    ReportedProblem = "[DEMO] Charger not powering the phone."
                });
                await _warranties.UpdateClaimAsync(submitted.Id, new UpdateWarrantyClaimRequest
                {
                    Status = "UnderInspection",
                    InspectionNotes = "[DEMO] Confirmed dead output."
                });
                await _warranties.UpdateClaimAsync(submitted.Id, new UpdateWarrantyClaimRequest
                {
                    Status = "Approved",
                    DecisionReason = "[DEMO] Manufacturing fault within shop policy."
                });
                await _warranties.UpdateClaimAsync(submitted.Id, new UpdateWarrantyClaimRequest
                {
                    Status = "ReplacementIssued",
                    DecisionReason = "[DEMO] Manufacturing fault within shop policy.",
                    ReplacementProductId = charger,
                    ReplacementSerial = "DEMO-SN-CHARGER-RPL"
                });
            }
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
            Message = "Sample grocery and Samsung catalogue is in place. Prices and DEMO-BC barcodes are fictional. Re-running this does not duplicate SKUs.",
            Products = new[]
            {
                "GRC-RICE-001", "GRC-DAL-001", "GRC-SUGAR-001", "GRC-ATTA-005",
                "GRC-OIL-001", "GRC-SALT-001", "GRC-BISC-001", "GRC-BREAD-001",
                "GRC-BUTTER-001", "GRC-WATER-001",
                "SAM-A06-464", "SAM-A16-8128", "SAM-A26-8128", "SAM-M35-8128", "SAM-S24FE-8128",
                "SAM-CHARGER-001", "SAM-CABLE-001", "SAM-COVER-001", "SAM-GLASS-001"
            },
            Note = "Opening stock is ledger OpeningStock (not doubled with a purchase). Internal barcodes are DEMO-BC-{SKU}. Test GSTINs 36AAAAA0000A1Z5 / 27AAAAA0000A1Z5 are fictional. Quantity is a whole packet/piece — not fractional kg/L."
        };
    }

    private async Task<int> EnsureTax(decimal percentage, string preferredName)
    {
        var existing = await _context.Taxes.AsNoTracking()
            .Where(t => !t.IsDeleted && t.IsActive && t.Percentage == percentage)
            .OrderBy(t => t.Name == preferredName ? 0 : 1)
            .Select(t => t.Id)
            .FirstOrDefaultAsync();
        if (existing != 0) return existing;

        var created = await _taxes.CreateAsync(new SaveTaxRequest
        {
            Name = preferredName,
            Percentage = percentage,
            Description = "Demo tax rate",
            IsActive = true
        });
        return created.Id;
    }

    private async Task<int> EnsureUnit(string name, string shortName)
    {
        var existing = await _context.Units.AsNoTracking()
            .Where(u => !u.IsDeleted && u.IsActive)
            .Where(u =>
                u.Name == name ||
                u.ShortName == shortName ||
                (name == "Piece" && (u.Name.Contains("Piece") || u.ShortName == "Pcs")))
            .OrderBy(u => u.Name == name ? 0 : 1)
            .Select(u => u.Id)
            .FirstOrDefaultAsync();
        if (existing != 0) return existing;

        var created = await _units.CreateAsync(new SaveUnitRequest
        {
            Name = name,
            ShortName = shortName,
            Description = "Demo unit (integer packets/pieces)",
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
        decimal cost, decimal price, int stock, string hsn,
        string? barcode = null, int? warrantyValue = null, string? warrantyUnit = null, bool serial = false)
    {
        var existing = await _context.Products
            .Where(p => !p.IsDeleted && p.SKU == sku)
            .FirstOrDefaultAsync();
        if (existing != null)
        {
            if (existing.Barcode == null && barcode != null)
                existing.Barcode = barcode;
            if (existing.WarrantyDurationValue == null && warrantyValue != null && warrantyUnit != null)
            {
                existing.WarrantyDurationValue = warrantyValue;
                existing.WarrantyDurationUnit = Enum.Parse<WarrantyDurationUnit>(warrantyUnit, true);
            }
            existing.RequiresSerialNumber = serial || existing.RequiresSerialNumber;
            await _context.SaveChangesAsync();
            return existing.Id;
        }

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
            HsnCode = hsn,
            Barcode = barcode,
            WarrantyDurationValue = warrantyValue,
            WarrantyDurationUnit = warrantyUnit,
            RequiresSerialNumber = serial
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

    private Task<bool> DemoSaleExists(string clientRequestId) =>
        _context.Sales.AsNoTracking()
            .AnyAsync(s => s.ClientRequestId == Guid.Parse(clientRequestId));

    private Task<bool> DemoReturnExists() =>
        _context.SalesReturns.AsNoTracking()
            .AnyAsync(r => r.Reason != null && r.Reason.Contains("[DEMO]"));
}
