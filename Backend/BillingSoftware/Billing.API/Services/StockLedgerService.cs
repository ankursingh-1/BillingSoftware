using Billing.Application.DTOs.StockLedger;
using Billing.Application.Interfaces;
using Billing.Persistence.Context;

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
        throw new NotImplementedException();
    }

    public async Task<List<StockLedgerDto>> GetByProductAsync(int productId)
    {
        throw new NotImplementedException();
    }
}