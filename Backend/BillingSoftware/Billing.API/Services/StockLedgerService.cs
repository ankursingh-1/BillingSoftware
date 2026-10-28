using Billing.API.Models;
using Billing.Application.Accounting;
using Billing.Application.DTOs.StockLedger;
using Billing.Domain.Entities;
using Billing.Domain.Enums;
using Billing.Persistence.Context;
using Billing.Shared.Exceptions;
using Microsoft.EntityFrameworkCore;

namespace Billing.API.Services;

public class StockLedgerService
{
    private const int MaxPageSize = 200;

    private readonly BillingDbContext _context;

    public StockLedgerService(BillingDbContext context)
    {
        _context = context;
    }

    public async Task<PagedResult<StockLedgerDto>> SearchAsync(StockLedgerSearchRequest request)
    {
        var pageNumber = Math.Max(request.PageNumber, 1);
        var pageSize = Math.Clamp(request.PageSize, 1, MaxPageSize);

        var query = _context.StockLedgers.AsNoTracking();

        if (request.ProductId.HasValue)
            query = query.Where(x => x.ProductId == request.ProductId.Value);

        if (!string.IsNullOrWhiteSpace(request.TransactionType))
        {
            if (!Enum.TryParse<StockTransactionType>(request.TransactionType, true, out var type))
                throw new BusinessException("Unknown transaction type filter.");

            query = query.Where(x => x.TransactionType == type);
        }

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var search = request.Search.Trim();

            query = query.Where(x =>
                x.Product.Name.Contains(search) ||
                x.Product.SKU.Contains(search) ||
                (x.ReferenceNo != null && x.ReferenceNo.Contains(search)));
        }

        // CreatedOn is stored in UTC; the filter dates are the user's local days.
        if (request.FromDate.HasValue)
        {
            var from = LocalDayStartUtc(request.FromDate.Value);
            query = query.Where(x => x.CreatedOn >= from);
        }

        if (request.ToDate.HasValue)
        {
            var to = LocalDayStartUtc(request.ToDate.Value.AddDays(1));
            query = query.Where(x => x.CreatedOn < to);
        }

        int totalRecords = await query.CountAsync();

        var rows = await query
            .OrderByDescending(x => x.Id)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new LedgerRow(
                x.Id,
                x.ProductId,
                x.Product.Name,
                x.Product.SKU,
                x.TransactionType,
                x.Quantity,
                x.PreviousStock,
                x.CurrentStock,
                x.ReferenceNo,
                x.Remarks,
                x.CreatedBy,
                x.CreatedOn))
            .ToListAsync();

        return new PagedResult<StockLedgerDto>
        {
            TotalRecords = totalRecords,
            TotalPages = (int)Math.Ceiling((double)totalRecords / pageSize),
            PageNumber = pageNumber,
            PageSize = pageSize,
            Data = rows.Select(ToDto).ToList()
        };
    }

    /// <summary>All movements of one product, newest first (kept for the product page).</summary>
    public async Task<List<StockLedgerDto>> GetByProductAsync(int productId)
    {
        var rows = await _context.StockLedgers
            .AsNoTracking()
            .Where(x => x.ProductId == productId)
            .OrderByDescending(x => x.Id)
            .Select(x => new LedgerRow(
                x.Id,
                x.ProductId,
                x.Product.Name,
                x.Product.SKU,
                x.TransactionType,
                x.Quantity,
                x.PreviousStock,
                x.CurrentStock,
                x.ReferenceNo,
                x.Remarks,
                x.CreatedBy,
                x.CreatedOn))
            .ToListAsync();

        return rows.Select(ToDto).ToList();
    }

    private sealed record LedgerRow(
        int Id,
        int ProductId,
        string ProductName,
        string Sku,
        StockTransactionType TransactionType,
        int Quantity,
        int PreviousStock,
        int CurrentStock,
        string? ReferenceNo,
        string? Remarks,
        string? CreatedBy,
        DateTime CreatedOn);

    private static StockLedgerDto ToDto(LedgerRow x) => new()
    {
        Id = x.Id,
        ProductId = x.ProductId,
        ProductName = x.ProductName,
        Sku = x.Sku,
        TransactionType = x.TransactionType.ToString(),
        Quantity = x.Quantity,
        QuantityChange = x.CurrentStock - x.PreviousStock,
        PreviousStock = x.PreviousStock,
        CurrentStock = x.CurrentStock,
        ReferenceNo = x.ReferenceNo,
        Remarks = x.Remarks,
        CreatedBy = x.CreatedBy,
        CreatedOn = x.CreatedOn.AsUtc()
    };

    internal static DateTime LocalDayStartUtc(DateTime day) =>
        DateTime.SpecifyKind(day.Date, DateTimeKind.Local).ToUniversalTime();
}