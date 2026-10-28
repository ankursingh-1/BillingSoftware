using Billing.API.Models;
using Billing.Application.Accounting;
using Billing.Application.DTOs.StockAdjustment;
using Billing.Application.Interfaces;
using Billing.Domain.Entities;
using Billing.Domain.Enums;
using Billing.Persistence.Context;
using Billing.Shared.Exceptions;
using Microsoft.EntityFrameworkCore;

namespace Billing.API.Services;

/// <summary>
/// Manual stock corrections (count fixes, damage, expiry, loss). Each one is a single
/// transaction: the atomic stock change, its ledger row, a reference number and an audit
/// entry commit together or not at all. Stock can never go below zero.
/// </summary>
public class StockAdjustmentService
{
    private const string Prefix = "ADJ";
    private const int MaxPageSize = 100;

    private static readonly StockTransactionType[] AdjustmentTypes =
    {
        StockTransactionType.Adjustment,
        StockTransactionType.AdjustmentOut,
        StockTransactionType.Damaged,
        StockTransactionType.Expired,
        StockTransactionType.Lost
    };

    private readonly BillingDbContext _context;
    private readonly IInventoryService _inventory;
    private readonly IDocumentNumberService _numbers;
    private readonly IAuditService _audit;

    public StockAdjustmentService(
        BillingDbContext context,
        IInventoryService inventory,
        IDocumentNumberService numbers,
        IAuditService audit)
    {
        _context = context;
        _inventory = inventory;
        _numbers = numbers;
        _audit = audit;
    }

    public async Task<StockAdjustmentResult> AdjustAsync(CreateStockAdjustmentDto dto)
    {
        var remarks = dto.Remarks?.Trim();

        if (string.IsNullOrEmpty(remarks) || remarks.Length < 3)
            throw new BusinessException("Explain why the stock is being adjusted (at least 3 characters).");

        if (dto.IncreaseStock && dto.Reason != StockAdjustmentReason.Correction)
            throw new BusinessException("Damaged, expired and lost stock can only be removed, not added.");

        var type = (dto.IncreaseStock, dto.Reason) switch
        {
            (true, _) => StockTransactionType.Adjustment,
            (false, StockAdjustmentReason.Correction) => StockTransactionType.AdjustmentOut,
            (false, StockAdjustmentReason.Damaged) => StockTransactionType.Damaged,
            (false, StockAdjustmentReason.Expired) => StockTransactionType.Expired,
            (false, StockAdjustmentReason.Lost) => StockTransactionType.Lost,
            _ => throw new BusinessException("Select a valid reason.")
        };

        await using var transaction = await _context.Database.BeginTransactionAsync();

        var product = await _context.Products
            .AsNoTracking()
            .Where(x => x.Id == dto.ProductId && !x.IsDeleted)
            .Select(x => new { x.Id, x.Name })
            .FirstOrDefaultAsync()
            ?? throw new BusinessException("Product not found or has been deleted.");

        var today = DateTime.Now;
        var sequence = await _numbers.NextAsync(DocumentNumbering.SeriesKey(Prefix, today));
        var reference = DocumentNumbering.Format(Prefix, today, sequence);

        // Throws a 409 if the removal would take stock below zero.
        await _inventory.AdjustStockAsync(product.Id, dto.Quantity, type, reference, remarks);

        var ledger = _context.ChangeTracker.Entries<StockLedger>()
            .Where(e => e.State == EntityState.Added)
            .Select(e => e.Entity)
            .Single();

        _audit.Record(
            "Stock.Adjust", "Product", product.Id.ToString(),
            $"{reference}; {type}; {ledger.PreviousStock} -> {ledger.CurrentStock}; {remarks}");

        await _context.SaveChangesAsync();
        await transaction.CommitAsync();

        return new StockAdjustmentResult
        {
            ReferenceNo = reference,
            ProductId = product.Id,
            PreviousStock = ledger.PreviousStock,
            CurrentStock = ledger.CurrentStock
        };
    }

    public async Task<PagedResult<StockAdjustmentDto>> GetHistoryAsync(StockAdjustmentSearchRequest request)
    {
        var pageNumber = Math.Max(request.PageNumber, 1);
        var pageSize = Math.Clamp(request.PageSize, 1, MaxPageSize);

        var query = _context.StockLedgers
            .AsNoTracking()
            .Where(x => AdjustmentTypes.Contains(x.TransactionType));

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var search = request.Search.Trim();

            query = query.Where(x =>
                x.Product.Name.Contains(search) ||
                x.Product.SKU.Contains(search) ||
                (x.ReferenceNo != null && x.ReferenceNo.Contains(search)));
        }

        if (request.FromDate.HasValue)
        {
            var from = StockLedgerService.LocalDayStartUtc(request.FromDate.Value);
            query = query.Where(x => x.CreatedOn >= from);
        }

        if (request.ToDate.HasValue)
        {
            var to = StockLedgerService.LocalDayStartUtc(request.ToDate.Value.AddDays(1));
            query = query.Where(x => x.CreatedOn < to);
        }

        int totalRecords = await query.CountAsync();

        var rows = await query
            .OrderByDescending(x => x.Id)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new
            {
                x.Id,
                x.ReferenceNo,
                x.ProductId,
                ProductName = x.Product.Name,
                Sku = x.Product.SKU,
                x.TransactionType,
                x.Quantity,
                x.PreviousStock,
                x.CurrentStock,
                x.Remarks,
                x.CreatedBy,
                x.CreatedOn
            })
            .ToListAsync();

        var data = rows.Select(x => new StockAdjustmentDto
        {
            Id = x.Id,
            ReferenceNo = x.ReferenceNo,
            ProductId = x.ProductId,
            ProductName = x.ProductName,
            Sku = x.Sku,
            Reason = ReasonLabel(x.TransactionType),
            Quantity = x.Quantity,
            IncreaseStock = x.TransactionType == StockTransactionType.Adjustment,
            PreviousStock = x.PreviousStock,
            CurrentStock = x.CurrentStock,
            Remarks = x.Remarks,
            CreatedBy = x.CreatedBy,
            AdjustmentDate = x.CreatedOn.AsUtc()
        }).ToList();

        return new PagedResult<StockAdjustmentDto>
        {
            TotalRecords = totalRecords,
            TotalPages = (int)Math.Ceiling((double)totalRecords / pageSize),
            PageNumber = pageNumber,
            PageSize = pageSize,
            Data = data
        };
    }

    private static string ReasonLabel(StockTransactionType type) => type switch
    {
        StockTransactionType.Adjustment or StockTransactionType.AdjustmentOut => "Correction",
        _ => type.ToString()
    };
}