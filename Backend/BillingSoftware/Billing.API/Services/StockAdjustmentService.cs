using Billing.Application.DTOs.StockAdjustment;
using Billing.Application.Interfaces;
using Billing.Persistence.Context;
using Billing.Domain.Entities;
using Billing.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Billing.API.Services;

public class StockAdjustmentService : IStockAdjustmentService
{
    private readonly BillingDbContext _context;
    private readonly IInventoryService _inventoryService;

    public StockAdjustmentService(BillingDbContext context,
    IInventoryService inventoryService)
    {
        _context = context;
        _inventoryService = inventoryService;
    }

    public async Task AdjustStockAsync(CreateStockAdjustmentDto dto)
    {
        var product = await _context.Products
            .FirstOrDefaultAsync(x => x.Id == dto.ProductId && !x.IsDeleted);

        if (product == null)
            throw new Exception("Product not found.");

        var transactionType = dto.IncreaseStock
            ? StockTransactionType.Adjustment
            : StockTransactionType.Damaged;

        await _inventoryService.AdjustStockAsync(
            dto.ProductId,
            dto.Quantity,
            transactionType,
            "STOCK-ADJ",
            dto.Remarks);

        await _context.SaveChangesAsync();
    }

    public async Task<List<StockAdjustmentDto>> GetHistoryAsync()
    {
        return await _context.StockLedgers
            .AsNoTracking()
            .Include(x => x.Product)
            .Where(x =>
                x.TransactionType == StockTransactionType.Adjustment ||
                x.TransactionType == StockTransactionType.Damaged)
            .OrderByDescending(x => x.CreatedOn)
            .Select(x => new StockAdjustmentDto
            {
                ProductId = x.ProductId,
                ProductName = x.Product.Name,
                Quantity = x.Quantity,
                IncreaseStock = x.TransactionType == StockTransactionType.Adjustment,
                Remarks = x.Remarks,
                AdjustmentDate = x.CreatedOn
            })
            .ToListAsync();
    }
}