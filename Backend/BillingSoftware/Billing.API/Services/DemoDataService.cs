using Billing.API.Models;
using Billing.Application.DTOs.Category;
using Billing.Application.DTOs.Expense;
using Billing.Application.DTOs.Purchases;
using Billing.Application.DTOs.Sales;
using Billing.Application.Interfaces;
using Billing.Domain.Enums;
using Billing.Persistence.Context;
using Billing.Shared.Exceptions;
using Microsoft.EntityFrameworkCore;

namespace Billing.API.Services;

/// <summary>
/// Fills clearly labelled DEMO records. Idempotent: existing DEMO SKUs / mobiles
/// are reused. Never touches records that are not labelled DEMO.
/// Disabled unless Demo:Enabled is true (and never in Production unless
/// Demo:AllowInProduction is also true).
/// </summary>
public class DemoDataService
{
    public const string Prefix = "DEMO";

    private readonly IHostEnvironment _environment;
    private readonly IConfiguration _configuration;
    private readonly BillingDbContext _context;
    private readonly CategoryService _categories;
    private readonly ProductService _products;
    private readonly CustomerService _customers;
    private readonly SupplierService _suppliers;
    private readonly PurchaseService _purchases;
    private readonly SaleService _sales;
    private readonly IExpenseService _expenses;

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
        IExpenseService expenses)
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
        _expenses = expenses;
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

        var taxId = await _context.Taxes.AsNoTracking()
            .Where(t => !t.IsDeleted && t.IsActive)
            .OrderBy(t => t.Percentage == 18 ? 0 : 1)
            .Select(t => (int?)t.Id)
            .FirstOrDefaultAsync();

        var category = await EnsureCategory();
        var usb = await EnsureProduct("DEMO USB cable", "DEMO-USB-01", category, taxId, 80, 120, 40);
        var hdmi = await EnsureProduct("DEMO HDMI cable", "DEMO-HDMI-01", category, taxId, 150, 220, 8);
        var low = await EnsureProduct("DEMO Low-stock item", "DEMO-LOW-01", category, taxId, 20, 40, 2);
        await EnsureProduct("DEMO Out of stock item", "DEMO-OUT-01", category, taxId, 10, 25, 0);

        var supplierId = await EnsureSupplier();
        var anita = await EnsureCustomer("DEMO - Anita Sharma", "7000000001", "36");
        var rahul = await EnsureCustomer("DEMO - Rahul Verma", "7000000002", "27");

        if (!await DemoPurchaseExists(usb))
        {
            await _purchases.CreateAsync(new CreatePurchaseRequest
            {
                SupplierId = supplierId,
                PurchaseDate = DateTime.Today.AddDays(-3),
                Remarks = "[DEMO] Opening purchase",
                ClientRequestId = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-000000000001"),
                Items = new List<CreatePurchaseItemRequest>
                {
                    new() { ProductId = usb, Quantity = 10, PurchasePrice = 80 },
                    new() { ProductId = hdmi, Quantity = 5, PurchasePrice = 150 }
                }
            });
        }

        if (!await DemoSaleExists(anita))
        {
            var created = await _sales.CreateAsync(new CreateSaleRequest
            {
                CustomerId = anita,
                Remarks = "[DEMO] Paid in full",
                ClientRequestId = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-000000000011"),
                Items = new List<CreateSaleItemRequest>
                {
                    new() { ProductId = usb, Quantity = 2 }
                }
            });

            var detail = await _sales.GetByIdAsync(created.SaleId);
            if (detail.Outstanding > 0)
            {
                await _sales.RecordPaymentAsync(created.SaleId, new RecordPaymentRequest
                {
                    Method = PaymentMethod.Upi,
                    Amount = detail.Outstanding,
                    Reference = "DEMO-UPI-1"
                });
            }
        }

        if (!await DemoSaleExists(rahul))
        {
            var created = await _sales.CreateAsync(new CreateSaleRequest
            {
                CustomerId = rahul,
                Remarks = "[DEMO] Partial payment",
                ClientRequestId = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-000000000012"),
                Items = new List<CreateSaleItemRequest>
                {
                    new() { ProductId = hdmi, Quantity = 1 }
                }
            });

            if (created.Outstanding > 50)
            {
                await _sales.RecordPaymentAsync(created.SaleId, new RecordPaymentRequest
                {
                    Method = PaymentMethod.Cash,
                    Amount = 50,
                    Reference = "DEMO-CASH-1"
                });
            }
        }

        if (!await _context.Expenses.AnyAsync(e => !e.IsDeleted && e.Title.StartsWith("DEMO ")))
        {
            await _expenses.CreateAsync(new CreateExpenseDto
            {
                Title = "DEMO shop rent",
                Amount = 15000,
                ExpenseDate = DateTime.Today.AddDays(-5),
                Category = "Rent",
                Remarks = "[DEMO]"
            });
        }

        return new
        {
            Success = true,
            Message = "Demo records are in place (labelled DEMO). Re-running this does not duplicate them.",
            Products = new[] { "DEMO-USB-01", "DEMO-HDMI-01", "DEMO-LOW-01", "DEMO-OUT-01" },
            Note = "Use a development copy of the database. Demo invoices stay in reports until you use a fresh copy."
        };
    }

    private async Task<int> EnsureCategory()
    {
        var existing = await _context.Categories.AsNoTracking()
            .Where(c => !c.IsDeleted && c.Name == "DEMO - Cables")
            .Select(c => c.Id)
            .FirstOrDefaultAsync();
        if (existing != 0) return existing;

        var created = await _categories.CreateAsync(new SaveCategoryRequest
        {
            Name = "DEMO - Cables",
            Description = "Demo category",
            IsActive = true
        });
        return created.Id;
    }

    private async Task<int> EnsureProduct(
        string name, string sku, int categoryId, int? taxId,
        decimal cost, decimal price, int stock)
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
            TaxId = taxId,
            HsnCode = "8517"
        });
    }

    private async Task<int> EnsureCustomer(string name, string mobile, string state)
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
            Address = "Demo address"
        });
    }

    private async Task<int> EnsureSupplier()
    {
        var existing = await _context.Suppliers.AsNoTracking()
            .Where(s => !s.IsDeleted && s.Mobile == "7000000099")
            .Select(s => s.Id)
            .FirstOrDefaultAsync();
        if (existing != 0) return existing;

        return await _suppliers.CreateAsync(new SupplierDto
        {
            Name = "DEMO contact",
            CompanyName = "DEMO Wholesale",
            Mobile = "7000000099",
            StateCode = "36"
        });
    }

    private Task<bool> DemoPurchaseExists(int productId) =>
        _context.PurchaseItems.AsNoTracking()
            .AnyAsync(i => i.ProductId == productId && i.Purchase.Remarks != null && i.Purchase.Remarks.Contains("[DEMO]"));

    private Task<bool> DemoSaleExists(int customerId) =>
        _context.Sales.AsNoTracking()
            .AnyAsync(s => s.CustomerId == customerId && s.Remarks != null && s.Remarks.Contains("[DEMO]"));
}
