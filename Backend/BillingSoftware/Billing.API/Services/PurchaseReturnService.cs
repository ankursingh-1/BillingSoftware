using Billing.Application.DTOs.PurchaseReturn;
using Billing.Application.Interfaces;
using Billing.Persistence.Context;
using Billing.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Billing.API.Services;

public class PurchaseReturnService : IPurchaseReturnService
{
    private readonly BillingDbContext _context;
    private readonly IInventoryService _inventoryService;

    public PurchaseReturnService(
        BillingDbContext context,
        IInventoryService inventoryService)
    {
        _context = context;
        _inventoryService = inventoryService;
    }

    public async Task CreateAsync(CreatePurchaseReturnDto dto)
    {
        var product = await _context.Products
            .FirstOrDefaultAsync(x => x.Id == dto.ProductId && !x.IsDeleted);

        if (product == null)
            throw new Exception("Product not found.");

        await _inventoryService.AdjustStockAsync(
            dto.ProductId,
            dto.Quantity,
            StockTransactionType.PurchaseReturn,
            dto.ReferenceNo,
            dto.Remarks);

        await _context.SaveChangesAsync();
    }

    public async Task<List<PurchaseReturnDto>> GetAllAsync()
    {
        return await _context.StockLedgers
            .AsNoTracking()
            .Include(x => x.Product)
            .Where(x => x.TransactionType == StockTransactionType.PurchaseReturn)
            .OrderByDescending(x => x.CreatedOn)
            .Select(x => new PurchaseReturnDto
            {
                ProductId = x.ProductId,
                ProductName = x.Product.Name,
                Quantity = x.Quantity,
                ReferenceNo = x.ReferenceNo,
                Remarks = x.Remarks,
                ReturnDate = x.CreatedOn
            })
            .ToListAsync();
    }
}