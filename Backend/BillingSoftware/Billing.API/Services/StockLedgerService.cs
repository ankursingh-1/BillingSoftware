using Billing.Application.DTOs.StockLedger;
using Billing.Application.Interfaces;
using Billing.Persistence.Context;
using Microsoft.EntityFrameworkCore;

namespace Billing.API.Services;

public class StockLedgerService : IStockLedgerService
{
    private readonly BillingDbContext _context;

    public StockLedgerService(BillingDbContext context)
    {
        _context = context;
    }

    public async Task<List<StockLedgerDto>> GetAllAsync()
    {
        return await _context.StockLedgers
            .AsNoTracking()
            .Include(x => x.Product)
            .OrderByDescending(x => x.CreatedOn)
            .Select(x => new StockLedgerDto
            {
                Id = x.Id,
                ProductId = x.ProductId,
                ProductName = x.Product.Name,
                TransactionType = x.TransactionType.ToString(),
                Quantity = x.Quantity,
                PreviousStock = x.PreviousStock,
                CurrentStock = x.CurrentStock,
                ReferenceNo = x.ReferenceNo,
                Remarks = x.Remarks,
                CreatedOn = x.CreatedOn
            })
            .ToListAsync();
    }

    public async Task<List<StockLedgerDto>> GetByProductAsync(int productId)
    {
        return await _context.StockLedgers
            .AsNoTracking()
            .Include(x => x.Product)
            .Where(x => x.ProductId == productId)
            .OrderByDescending(x => x.CreatedOn)
            .Select(x => new StockLedgerDto
            {
                Id = x.Id,
                ProductId = x.ProductId,
                ProductName = x.Product.Name,
                TransactionType = x.TransactionType.ToString(),
                Quantity = x.Quantity,
                PreviousStock = x.PreviousStock,
                CurrentStock = x.CurrentStock,
                ReferenceNo = x.ReferenceNo,
                Remarks = x.Remarks,
                CreatedOn = x.CreatedOn
            })
            .ToListAsync();
    }
}