using Billing.Application.DTOs.Sales;
using Billing.Domain.Entities;
using Billing.Persistence.Context;
using Microsoft.EntityFrameworkCore;
using Billing.Application.Interfaces;
using Billing.Domain.Enums;

namespace Billing.API.Services;

public class SaleService
{
    private readonly BillingDbContext _context;
    private readonly IInventoryService _inventoryService;

    public SaleService(
    BillingDbContext context,
    IInventoryService inventoryService)
    {
        _context = context;
        _inventoryService = inventoryService;
    }

    public async Task<int> CreateAsync(CreateSaleRequest dto)
    {
        using var transaction = await _context.Database.BeginTransactionAsync();

        try
        {
            var sale = new Sale
            {
                InvoiceNumber = await GenerateInvoiceNumber(),
                CustomerId = dto.CustomerId,
                SaleDate = DateTime.Now,
                Remarks = null,
                TotalAmount = 0
            };

            _context.Sales.Add(sale);

            await _context.SaveChangesAsync();

            decimal grandTotal = 0;

            foreach (var item in dto.Items)
            {
                var product = await _context.Products
                    .FirstOrDefaultAsync(x =>
                        x.Id == item.ProductId &&
                        !x.IsDeleted);

                if (product == null)
                    throw new Exception($"Product {item.ProductId} not found.");

                if (product.Stock < item.Quantity)
                    throw new Exception($"{product.Name} stock is not available.");

                decimal lineTotal = item.Quantity * item.UnitPrice;

                var saleItem = new SaleItem
                {
                    SaleId = sale.Id,
                    ProductId = item.ProductId,
                    Quantity = item.Quantity,
                    SellingPrice = item.UnitPrice,
                    Total = lineTotal
                };

                _context.SaleItems.Add(saleItem);

                await _inventoryService.AdjustStockAsync(
                    product.Id,
                    item.Quantity,
                    StockTransactionType.Sale,
                    sale.InvoiceNumber,
                    "Sales Entry");

                grandTotal += lineTotal;
            }
            sale.TotalAmount = grandTotal;
            await _context.SaveChangesAsync();
            await transaction.CommitAsync();
            return sale.Id;
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    private async Task<string> GenerateInvoiceNumber()
    {
        int count = await _context.Sales.CountAsync() + 1;
        return $"INV-{DateTime.Now:yyyyMMdd}-{count:D6}";
    }
}