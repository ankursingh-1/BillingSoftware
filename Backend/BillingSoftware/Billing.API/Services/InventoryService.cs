using Billing.Application.Interfaces;
using Billing.Domain.Entities;
using Billing.Domain.Enums;
using Billing.Persistence.Context;
using Billing.Shared.Exceptions;
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

        /// <summary>
        /// Moves stock and writes the matching stock-ledger row.
        ///
        /// The stock change is ONE conditional UPDATE (Stock + delta, only if the result
        /// is not negative), so two requests can never both take the last unit and no
        /// update is ever lost, whatever else is running. Must be called inside a
        /// transaction; the ledger row is saved by the caller's SaveChanges.
        /// </summary>
        public async Task AdjustStockAsync(
            int productId,
            int quantity,
            StockTransactionType transactionType,
            string? referenceNo = null,
            string? remarks = null)
        {
            if (quantity <= 0)
                throw new BusinessException("Quantity must be greater than zero.");

            var delta = transactionType switch
            {
                StockTransactionType.Purchase or
                StockTransactionType.SalesReturn or
                StockTransactionType.OpeningStock or
                StockTransactionType.Adjustment => quantity,

                StockTransactionType.Sale or
                StockTransactionType.PurchaseReturn or
                StockTransactionType.Damaged or
                StockTransactionType.Expired or
                StockTransactionType.Lost => -quantity,

                _ => throw new BusinessException("Invalid stock transaction type.")
            };

            const string sql = @"
UPDATE Products
   SET Stock = Stock + @delta
OUTPUT deleted.Stock, inserted.Stock
 WHERE Id = @id AND IsDeleted = 0 AND Stock + @delta >= 0;";

            int previousStock;
            int currentStock;

            await using (var command = SqlTransactionHelper.CreateCommand(
                _context, sql, ("@delta", delta), ("@id", productId)))
            await using (var reader = await command.ExecuteReaderAsync())
            {
                if (!await reader.ReadAsync())
                {
                    await reader.CloseAsync();
                    await ThrowWhyStockChangeFailedAsync(productId, quantity);
                    return;
                }

                previousStock = reader.GetInt32(0);
                currentStock = reader.GetInt32(1);
            }

            // Keep an already-loaded Product instance consistent with the database.
            var tracked = _context.ChangeTracker.Entries<Product>()
                .FirstOrDefault(e => e.Entity.Id == productId);

            if (tracked != null)
            {
                var stock = tracked.Property(p => p.Stock);
                stock.CurrentValue = currentStock;
                stock.OriginalValue = currentStock;
                stock.IsModified = false;
            }

            _context.StockLedgers.Add(new StockLedger
            {
                ProductId = productId,
                TransactionType = transactionType,
                Quantity = quantity,
                PreviousStock = previousStock,
                CurrentStock = currentStock,
                ReferenceNo = referenceNo,
                Remarks = remarks ?? transactionType.ToString(),
                CreatedOn = DateTime.UtcNow
            });
        }

        private async Task ThrowWhyStockChangeFailedAsync(int productId, int quantity)
        {
            var product = await _context.Products
                .AsNoTracking()
                .Where(x => x.Id == productId && !x.IsDeleted)
                .Select(x => new { x.Name, x.Stock })
                .FirstOrDefaultAsync();

            if (product == null)
                throw new BusinessException($"Product not found or has been deleted (id: {productId}).");

            throw BusinessException.Conflict(
                $"Insufficient stock for '{product.Name}': {product.Stock} available, {quantity} requested.");
        }
    }
}
