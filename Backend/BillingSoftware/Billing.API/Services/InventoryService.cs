using Billing.Application.Interfaces;
using Billing.Domain.Entities;
using Billing.Domain.Enums;
using Billing.Persistence.Context;
using Microsoft.EntityFrameworkCore;

namespace Billing.API.Services
{
    public class InventoryService : IInventoryService
    {
        private readonly BillingDbContext _context;

        public InventoryService(BillingDbContext context)
        {
            _context = context;
        }

        public async Task AdjustStockAsync(
        int productId,int quantity,
        StockTransactionType transactionType,
        string? referenceNo = null,
        string? remarks = null)
        {
            if (quantity <= 0)
                throw new Exception("Quantity must be greater than zero.");

            var product = await _context.Products
            .FirstOrDefaultAsync(x => x.Id == productId && !x.IsDeleted);

            if (product == null)
                throw new Exception("Product not found.");

            int previousStock = product.Stock;

            switch (transactionType)
            {
                case StockTransactionType.Purchase:
                case StockTransactionType.SalesReturn:
                case StockTransactionType.OpeningStock:
                case StockTransactionType.Adjustment:
                    product.Stock += quantity;
                    break;

                case StockTransactionType.Sale:
                case StockTransactionType.PurchaseReturn:
                case StockTransactionType.Damaged:
                case StockTransactionType.Expired:
                case StockTransactionType.Lost:

                    if (product.Stock < quantity)
                        throw new Exception("Insufficient stock.");

                    product.Stock -= quantity;
                    break;

                default:
                    throw new Exception("Invalid transaction type.");
            }

            var ledger = new StockLedger
            {
                ProductId = product.Id,
                TransactionType = transactionType,
                Quantity = quantity,
                PreviousStock = previousStock,
                CurrentStock = product.Stock,
                ReferenceNo = referenceNo,
                Remarks = remarks ?? transactionType.ToString(),
                CreatedOn = DateTime.UtcNow
            };
            _context.StockLedgers.Add(ledger);
            Console.WriteLine($"Stock Ledger Added -> ProductId={product.Id}, Qty={quantity}, Type={transactionType}");
        }
    }
}