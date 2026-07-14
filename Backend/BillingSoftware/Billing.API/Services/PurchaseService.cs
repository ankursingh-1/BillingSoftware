using Billing.API.Models;
using Billing.Domain.Entities;
using Billing.Persistence.Context;
using Microsoft.EntityFrameworkCore;

namespace Billing.API.Services;

public class PurchaseService
{
    private readonly BillingDbContext _context;

    public PurchaseService(BillingDbContext context)
    {
        _context = context;
    }

    public async Task<int> CreateAsync(CreatePurchaseDto dto)
    {
        using var transaction = await _context.Database.BeginTransactionAsync();

        try
        {
            var purchase = new Purchase
            {
                PurchaseNumber = await GeneratePurchaseNumber(),
                SupplierId = dto.SupplierId,
                PurchaseDate = dto.PurchaseDate,
                Remarks = dto.Remarks,
                TotalAmount = 0
            };

            _context.Purchases.Add(purchase);

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

                decimal lineTotal = item.Quantity * item.PurchasePrice;

                var purchaseItem = new PurchaseItem
                {
                    PurchaseId = purchase.Id,
                    ProductId = item.ProductId,
                    Quantity = item.Quantity,
                    PurchasePrice = item.PurchasePrice,
                    Total = lineTotal
                };

                _context.PurchaseItems.Add(purchaseItem);

                product.Stock += item.Quantity;

                grandTotal += lineTotal;
            }

            purchase.TotalAmount = grandTotal;

            await _context.SaveChangesAsync();

            await transaction.CommitAsync();

            return purchase.Id;
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    private async Task<string> GeneratePurchaseNumber()
    {
        int count = await _context.Purchases.CountAsync() + 1;

        return $"PUR-{DateTime.Now:yyyyMMdd}-{count:D6}";
    }
}