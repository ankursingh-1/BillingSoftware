using Billing.Application.DTOs.SalesReturn;
using Billing.Application.Interfaces;
using Billing.Domain.Enums;
using Billing.Persistence.Context;
using Microsoft.EntityFrameworkCore;

namespace Billing.API.Services;

public class SalesReturnService : ISalesReturnService
{
    private readonly BillingDbContext _context;
    private readonly IInventoryService _inventoryService;

    public SalesReturnService(
        BillingDbContext context,
        IInventoryService inventoryService)
    {
        _context = context;
        _inventoryService = inventoryService;
    }

    public async Task CreateAsync(CreateSalesReturnDto dto)
    {
        var product = await _context.Products
            .FirstOrDefaultAsync(x => x.Id == dto.ProductId && !x.IsDeleted);

        if (product == null)
            throw new Exception("Product not found.");

        await _inventoryService.AdjustStockAsync(
            dto.ProductId,
            dto.Quantity,
            StockTransactionType.SalesReturn,
            dto.ReferenceNo,
            dto.Remarks);

        await _context.SaveChangesAsync();
    }

    public async Task<List<SalesReturnDto>> GetAllAsync()
    {
        return await _context.StockLedgers
            .AsNoTracking()
            .Include(x => x.Product)
            .Where(x => x.TransactionType == StockTransactionType.SalesReturn)
            .OrderByDescending(x => x.CreatedOn)
            .Select(x => new SalesReturnDto
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