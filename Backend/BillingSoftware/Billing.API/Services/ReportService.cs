using Billing.Application.DTOs.Reports;
using Billing.Persistence.Context;
using Microsoft.EntityFrameworkCore;

namespace Billing.API.Services;

public class ReportService
{
    private readonly BillingDbContext _context;

    public ReportService(BillingDbContext context)
    {
        _context = context;
    }

    public async Task<SalesReportResponse> GetSalesReportAsync(SalesReportRequest request)
    {
        var query = _context.Sales
            .AsNoTracking()
            .Include(x => x.Customer)
            .AsQueryable();

        if (request.FromDate.HasValue)
        {
            query = query.Where(x => x.SaleDate >= request.FromDate.Value);
        }

        if (request.ToDate.HasValue)
        {
            query = query.Where(x => x.SaleDate <= request.ToDate.Value);
        }

        if (request.CustomerId.HasValue)
        {
            query = query.Where(x => x.CustomerId == request.CustomerId.Value);
        }

        if (!string.IsNullOrWhiteSpace(request.InvoiceNumber))
        {
            query = query.Where(x =>
                x.InvoiceNumber.Contains(request.InvoiceNumber));
        }

        var totalRecords = await query.CountAsync();

        var grandTotal = await query.SumAsync(x => x.TotalAmount);

        var items = await query
            .OrderByDescending(x => x.SaleDate)
            .Skip((request.PageNumber - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(x => new SalesReportItemDto
            {
                SaleId = x.Id,
                InvoiceNumber = x.InvoiceNumber,
                CustomerName = x.Customer.Name,
                SaleDate = x.SaleDate,
                TotalAmount = x.TotalAmount
            })
            .ToListAsync();

        return new SalesReportResponse
        {
            PageNumber = request.PageNumber,
            PageSize = request.PageSize,
            TotalRecords = totalRecords,
            TotalPages = (int)Math.Ceiling((double)totalRecords / request.PageSize),
            GrandTotal = grandTotal,
            Items = items
        };
    }
}