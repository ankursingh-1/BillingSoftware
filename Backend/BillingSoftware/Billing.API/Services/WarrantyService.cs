using Billing.Application.Accounting;
using Billing.Application.DTOs.Common;
using Billing.Application.DTOs.Warranty;
using Billing.Application.Interfaces;
using Billing.Application.Warranty;
using Billing.Domain.Entities;
using Billing.Domain.Enums;
using Billing.Persistence;
using Billing.Persistence.Context;
using Billing.Shared.Exceptions;
using Microsoft.EntityFrameworkCore;

namespace Billing.API.Services;

public class WarrantyService : IWarrantyService
{
    private const int MaxPageSize = 100;

    private readonly BillingDbContext _context;
    private readonly IInventoryService _inventory;
    private readonly IDocumentNumberService _numbers;
    private readonly IAuditService _audit;
    private readonly ICurrentUser _user;

    public WarrantyService(
        BillingDbContext context,
        IInventoryService inventory,
        IDocumentNumberService numbers,
        IAuditService audit,
        ICurrentUser user)
    {
        _context = context;
        _inventory = inventory;
        _numbers = numbers;
        _audit = audit;
        _user = user;
    }

    public async Task<PagedResult<WarrantyListItemDto>> SearchAsync(WarrantySearchRequest request)
    {
        var pageSize = Math.Clamp(request.PageSize, 1, MaxPageSize);
        var pageNumber = Math.Max(request.PageNumber, 1);
        var today = DateTime.Today;

        var query = Filter(request);

        var total = await query.CountAsync();

        var rows = await query
            .OrderByDescending(w => w.EndDate)
            .ThenBy(w => w.Id)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return new PagedResult<WarrantyListItemDto>
        {
            PageNumber = pageNumber,
            PageSize = pageSize,
            TotalRecords = total,
            TotalPages = (int)Math.Ceiling(total / (double)pageSize),
            Items = rows.Select(w => ToListItem(w, today)).ToList()
        };
    }

    public async Task<WarrantyDetailDto> GetByIdAsync(int id)
    {
        var warranty = await _context.Warranties
            .AsNoTracking()
            .Include(w => w.Claims)
                .ThenInclude(c => c.Events)
            .FirstOrDefaultAsync(w => w.Id == id)
            ?? throw BusinessException.NotFound("Warranty not found.");

        return ToDetail(warranty, DateTime.Today);
    }

    public async Task<List<WarrantyListItemDto>> ExpiringAsync(int take = 10)
    {
        var today = DateTime.Today;
        var until = today.AddDays(WarrantyDates.ExpiringSoonDays);

        try
        {
            var (from, toExclusive) = WarrantyDates.DayWindow(today, until);

            var rows = await _context.Warranties
                .AsNoTracking()
                .Where(w => !w.IsVoided && w.EndDate >= from && w.EndDate < toExclusive)
                .OrderBy(w => w.EndDate)
                .Take(Math.Clamp(take, 1, 50))
                .ToListAsync();

            return rows.Select(w => ToListItem(w, today)).ToList();
        }
        catch (Exception ex) when (SqlServerErrors.IsMissingObject(ex))
        {
            return [];
        }
    }

    public async Task<int> CountExpiringAsync()
    {
        var today = DateTime.Today;
        var until = today.AddDays(WarrantyDates.ExpiringSoonDays);
        var (from, toExclusive) = WarrantyDates.DayWindow(today, until);

        try
        {
            return await _context.Warranties
                .AsNoTracking()
                .CountAsync(w => !w.IsVoided && w.EndDate >= from && w.EndDate < toExclusive);
        }
        catch (Exception ex) when (SqlServerErrors.IsMissingObject(ex))
        {
            return 0;
        }
    }

    public async Task<PagedResult<WarrantyListItemDto>> ReportAsync(WarrantyReportRequest request)
    {
        var today = DateTime.Today;
        var kind = (request.Kind ?? "active").Trim().ToLowerInvariant();

        var query = _context.Warranties.AsNoTracking().Where(w => !w.IsVoided);

        var expiringUntil = today.AddDays(WarrantyDates.ExpiringSoonDays);
        var (expiringFrom, expiringTo) = WarrantyDates.DayWindow(today, expiringUntil);

        query = kind switch
        {
            "expired" => query.Where(w => w.EndDate < today),
            "expiring" => query.Where(w => w.EndDate >= expiringFrom && w.EndDate < expiringTo),
            _ => query.Where(w => w.EndDate >= today)
        };

        if (request.FromDate is { } from)
            query = query.Where(w => w.StartDate >= from.Date);
        if (request.ToDate is { } to)
            query = query.Where(w => w.StartDate < to.Date.AddDays(1));

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var search = request.Search.Trim();
            query = query.Where(w =>
                w.WarrantyNumber.Contains(search) ||
                w.InvoiceNumber.Contains(search) ||
                w.CustomerMobile.Contains(search) ||
                w.CustomerName.Contains(search) ||
                w.ProductName.Contains(search) ||
                (w.SerialNumber != null && w.SerialNumber.Contains(search)));
        }

        var pageSize = Math.Clamp(request.PageSize, 1, MaxPageSize);
        var pageNumber = Math.Max(request.PageNumber, 1);
        var total = await query.CountAsync();
        var rows = await query
            .OrderBy(w => w.EndDate)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return new PagedResult<WarrantyListItemDto>
        {
            PageNumber = pageNumber,
            PageSize = pageSize,
            TotalRecords = total,
            TotalPages = (int)Math.Ceiling(total / (double)pageSize),
            Items = rows.Select(w => ToListItem(w, today)).ToList()
        };
    }

    public async Task<PagedResult<WarrantyClaimReportItemDto>> ClaimReportAsync(WarrantyReportRequest request)
    {
        var kind = (request.Kind ?? "claims").Trim().ToLowerInvariant();

        var query = _context.WarrantyClaims
            .AsNoTracking()
            .Include(c => c.Warranty)
            .AsQueryable();

        query = kind switch
        {
            "approved" => query.Where(c => c.Status == WarrantyClaimStatus.Approved
                || c.Status == WarrantyClaimStatus.Repaired
                || c.Status == WarrantyClaimStatus.ReplacementIssued),
            "rejected" => query.Where(c => c.Status == WarrantyClaimStatus.Rejected),
            "replacements" => query.Where(c => c.Status == WarrantyClaimStatus.ReplacementIssued),
            "inspection" => query.Where(c => c.Status == WarrantyClaimStatus.UnderInspection),
            _ => query
        };

        if (request.FromDate is { } from)
            query = query.Where(c => c.SubmittedOn >= from.Date);
        if (request.ToDate is { } to)
            query = query.Where(c => c.SubmittedOn < to.Date.AddDays(1));

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var search = request.Search.Trim();
            query = query.Where(c =>
                c.ClaimNumber.Contains(search) ||
                c.Warranty.WarrantyNumber.Contains(search) ||
                c.Warranty.InvoiceNumber.Contains(search) ||
                c.Warranty.CustomerName.Contains(search) ||
                c.Warranty.ProductName.Contains(search));
        }

        var pageSize = Math.Clamp(request.PageSize, 1, MaxPageSize);
        var pageNumber = Math.Max(request.PageNumber, 1);
        var total = await query.CountAsync();
        var rows = await query
            .OrderByDescending(c => c.SubmittedOn)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return new PagedResult<WarrantyClaimReportItemDto>
        {
            PageNumber = pageNumber,
            PageSize = pageSize,
            TotalRecords = total,
            TotalPages = (int)Math.Ceiling(total / (double)pageSize),
            Items = rows.Select(c => new WarrantyClaimReportItemDto
            {
                WarrantyId = c.WarrantyId,
                SaleId = c.Warranty.SaleId,
                ClaimNumber = c.ClaimNumber,
                WarrantyNumber = c.Warranty.WarrantyNumber,
                InvoiceNumber = c.Warranty.InvoiceNumber,
                CustomerName = c.Warranty.CustomerName,
                ProductName = c.Warranty.ProductName,
                Status = c.Status.ToString(),
                SubmittedOn = c.SubmittedOn,
                ReplacementProductName = c.ReplacementProductName
            }).ToList()
        };
    }

    public async Task<WarrantyClaimDto> SubmitClaimAsync(int warrantyId, SubmitWarrantyClaimRequest request)
    {
        var problem = request.ReportedProblem?.Trim() ?? "";
        if (problem.Length == 0)
            throw new BusinessException("Describe the reported problem.");
        if (problem.Length > 1000)
            throw new BusinessException("The reported problem cannot exceed 1000 characters.");

        await using var transaction = await _context.Database.BeginTransactionAsync();

        var warranty = await _context.Warranties
            .Include(w => w.Claims)
            .FirstOrDefaultAsync(w => w.Id == warrantyId)
            ?? throw BusinessException.NotFound("Warranty not found.");

        if (warranty.IsVoided)
            throw BusinessException.Conflict("This warranty registration is voided.");

        if (!WarrantyDates.IsEligible(warranty.StartDate, warranty.EndDate, DateTime.Today, warranty.IsVoided))
            throw BusinessException.Conflict(
                "This warranty is not currently eligible. The dates are outside the stored warranty period or it has expired.");

        if (warranty.Claims.Any(c => WarrantyClaimTransitions.IsOpen(c.Status)))
            throw BusinessException.Conflict(
                "This warranty already has an open claim. Close or reject it before submitting another.");

        var now = DateTime.UtcNow;
        var today = DateTime.Today;
        var sequence = await _numbers.NextAsync(DocumentNumbering.ClaimSeriesKey(today));

        var claim = new WarrantyClaim
        {
            WarrantyId = warranty.Id,
            ClaimNumber = DocumentNumbering.Format(DocumentNumbering.ClaimPrefix, today, sequence),
            Status = WarrantyClaimStatus.Submitted,
            ReportedProblem = problem,
            Notes = string.IsNullOrWhiteSpace(request.Notes) ? null : request.Notes.Trim(),
            SubmittedOn = now,
            CreatedOn = now,
            CreatedBy = _user.Email
        };

        claim.Events.Add(new WarrantyClaimEvent
        {
            Status = WarrantyClaimStatus.Submitted,
            Notes = claim.Notes,
            CreatedOn = now,
            CreatedBy = _user.Email
        });

        _context.WarrantyClaims.Add(claim);

        _audit.Record(
            "WarrantyClaim.Submit", "Warranty", warranty.Id.ToString(),
            $"{claim.ClaimNumber}; {warranty.WarrantyNumber}");

        await _context.SaveChangesAsync();
        await transaction.CommitAsync();

        return ToClaimDto(claim);
    }

    public async Task<WarrantyClaimDto> UpdateClaimAsync(int claimId, UpdateWarrantyClaimRequest request)
    {
        if (!Enum.TryParse<WarrantyClaimStatus>(request.Status, true, out var target)
            || !Enum.IsDefined(target))
            throw new BusinessException(
                "Unknown claim status. Use Submitted, UnderInspection, Approved, Rejected, Repaired, ReplacementIssued or Closed.");

        await using var transaction = await _context.Database.BeginTransactionAsync();

        var claim = await _context.WarrantyClaims
            .Include(c => c.Warranty)
            .Include(c => c.Events)
            .FirstOrDefaultAsync(c => c.Id == claimId)
            ?? throw BusinessException.NotFound("Warranty claim not found.");

        if (!WarrantyClaimTransitions.CanMove(claim.Status, target))
            throw BusinessException.Conflict(
                $"Cannot move a {claim.Status} claim to {target}.");

        if (!string.IsNullOrWhiteSpace(request.InspectionNotes))
            claim.InspectionNotes = request.InspectionNotes.Trim();
        if (!string.IsNullOrWhiteSpace(request.DecisionReason))
            claim.DecisionReason = request.DecisionReason.Trim();
        if (!string.IsNullOrWhiteSpace(request.Notes))
            claim.Notes = request.Notes.Trim();
        if (!string.IsNullOrWhiteSpace(request.DefectiveSerial))
            claim.DefectiveSerial = request.DefectiveSerial.Trim();

        if (target is WarrantyClaimStatus.Approved or WarrantyClaimStatus.Rejected)
        {
            if (string.IsNullOrWhiteSpace(claim.DecisionReason))
                throw new BusinessException("Record the decision reason before approving or rejecting the claim.");
        }

        if (target == WarrantyClaimStatus.ReplacementIssued)
        {
            await IssueReplacementAsync(claim, request);
        }

        if (target != claim.Status)
        {
            claim.Status = target;
            if (WarrantyClaimTransitions.IsTerminal(target))
                claim.ResolvedOn ??= DateTime.UtcNow;

            claim.Events.Add(new WarrantyClaimEvent
            {
                Status = target,
                Notes = request.Notes?.Trim() ?? request.DecisionReason?.Trim() ?? request.InspectionNotes?.Trim(),
                CreatedOn = DateTime.UtcNow,
                CreatedBy = _user.Email
            });
        }

        claim.modifieson = DateTime.UtcNow;
        claim.ModifiedBy = _user.Email;

        _audit.Record(
            "WarrantyClaim.Update", "WarrantyClaim", claim.Id.ToString(),
            $"{claim.ClaimNumber}; {claim.Status}");

        await _context.SaveChangesAsync();
        await transaction.CommitAsync();

        return ToClaimDto(claim);
    }

    private async Task IssueReplacementAsync(WarrantyClaim claim, UpdateWarrantyClaimRequest request)
    {
        if (claim.ReplacementStockMoved)
            throw BusinessException.Conflict(
                "A replacement has already been issued for this claim. Stock was not deducted again.");

        if (claim.Status != WarrantyClaimStatus.Approved)
            throw BusinessException.Conflict("Approve the claim before issuing a replacement.");

        var productId = request.ReplacementProductId ?? claim.Warranty.ProductId;
        var product = await _context.Products
            .FirstOrDefaultAsync(p => p.Id == productId && !p.IsDeleted)
            ?? throw new BusinessException("Replacement product was not found.");

        var serial = string.IsNullOrWhiteSpace(request.ReplacementSerial)
            ? null
            : request.ReplacementSerial.Trim();

        if (product.RequiresSerialNumber && serial == null)
            throw new BusinessException("Enter the replacement product serial number.");

        if (serial != null)
        {
            var taken = await _context.Warranties.AnyAsync(w =>
                w.SerialNumber == serial && !w.IsVoided);
            if (taken)
                throw BusinessException.Conflict($"Serial number '{serial}' is already registered on another warranty.");
        }

        await _inventory.AdjustStockAsync(
            product.Id,
            1,
            StockTransactionType.WarrantyReplacement,
            claim.ClaimNumber,
            $"Warranty replacement for {claim.Warranty.WarrantyNumber}; original invoice {claim.Warranty.InvoiceNumber}");

        claim.ReplacementProductId = product.Id;
        claim.ReplacementProductName = product.Name;
        claim.ReplacementSerial = serial;
        claim.ReplacementStockMoved = true;

        if (string.IsNullOrWhiteSpace(claim.DefectiveSerial))
            claim.DefectiveSerial = claim.Warranty.SerialNumber;
    }

    private IQueryable<Domain.Entities.Warranty> Filter(WarrantySearchRequest request)
    {
        var query = _context.Warranties.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var search = request.Search.Trim();
            query = query.Where(w =>
                w.WarrantyNumber.Contains(search) ||
                w.InvoiceNumber.Contains(search) ||
                w.CustomerMobile.Contains(search) ||
                w.CustomerName.Contains(search) ||
                w.ProductName.Contains(search) ||
                (w.SerialNumber != null && w.SerialNumber.Contains(search)));
        }

        if (request.FromDate is { } from)
            query = query.Where(w => w.StartDate >= from.Date);
        if (request.ToDate is { } to)
            query = query.Where(w => w.StartDate < to.Date.AddDays(1));

        if (!string.IsNullOrWhiteSpace(request.Status))
        {
            var today = DateTime.Today;
            var status = request.Status.Trim();
            var (expiringFrom, expiringTo) = WarrantyDates.DayWindow(
                today,
                today.AddDays(WarrantyDates.ExpiringSoonDays));
            var activeFrom = today.AddDays(WarrantyDates.ExpiringSoonDays + 1);

            query = status.ToLowerInvariant() switch
            {
                "expired" => query.Where(w => !w.IsVoided && w.EndDate < today),
                "expiringsoon" or "expiring" => query.Where(w =>
                    !w.IsVoided && w.EndDate >= expiringFrom && w.EndDate < expiringTo),
                "active" => query.Where(w => !w.IsVoided && w.EndDate >= activeFrom),
                "voided" => query.Where(w => w.IsVoided),
                _ => query
            };
        }

        return query;
    }

    private static WarrantyListItemDto ToListItem(Domain.Entities.Warranty w, DateTime today)
    {
        var days = WarrantyDates.DaysRemaining(w.EndDate, today);
        var eligible = WarrantyDates.IsEligible(w.StartDate, w.EndDate, today, w.IsVoided);

        return new WarrantyListItemDto
        {
            Id = w.Id,
            WarrantyNumber = w.WarrantyNumber,
            InvoiceNumber = w.InvoiceNumber,
            SaleId = w.SaleId,
            ProductName = w.ProductName,
            SerialNumber = w.SerialNumber,
            CustomerName = w.CustomerName,
            CustomerMobile = w.CustomerMobile,
            Duration = WarrantyDates.FormatDuration(w.DurationValue, w.DurationUnit),
            StartDate = w.StartDate,
            EndDate = w.EndDate,
            DaysRemaining = days,
            Status = w.IsVoided ? "Voided" : WarrantyDates.Status(w.EndDate, today),
            Eligible = eligible
        };
    }

    private static WarrantyDetailDto ToDetail(Domain.Entities.Warranty w, DateTime today)
    {
        var item = ToListItem(w, today);
        var completed = w.Claims.Any(c =>
            c.Status is WarrantyClaimStatus.Repaired
                or WarrantyClaimStatus.ReplacementIssued
                or WarrantyClaimStatus.Closed);
        var eligible = item.Eligible && !completed;

        var note = !item.Eligible
            ? "Not eligible: the stored warranty dates are outside the period or the registration is voided. Dates are taken from this registration, not from the product's current default."
            : completed
                ? "Not eligible for a new claim: a previous claim on this registration was repaired, replaced or closed."
                : "Eligible by date only. Inspect the product and record the decision — a date in range is not an automatic replacement.";

        return new WarrantyDetailDto
        {
            Id = item.Id,
            WarrantyNumber = item.WarrantyNumber,
            InvoiceNumber = item.InvoiceNumber,
            SaleId = item.SaleId,
            SaleItemId = w.SaleItemId,
            ProductId = w.ProductId,
            ProductName = item.ProductName,
            ProductSku = w.ProductSku,
            SerialNumber = item.SerialNumber,
            CustomerId = w.CustomerId,
            CustomerName = item.CustomerName,
            CustomerMobile = item.CustomerMobile,
            Duration = item.Duration,
            DurationValue = w.DurationValue,
            DurationUnit = w.DurationUnit.ToString(),
            StartDate = item.StartDate,
            EndDate = item.EndDate,
            DaysRemaining = item.DaysRemaining,
            Status = item.Status,
            Eligible = eligible,
            Notes = w.Notes,
            IsVoided = w.IsVoided,
            EligibilityNote = note,
            Claims = w.Claims
                .OrderBy(c => c.Id)
                .Select(ToClaimDto)
                .ToList()
        };
    }

    private static WarrantyClaimDto ToClaimDto(WarrantyClaim c) => new()
    {
        Id = c.Id,
        ClaimNumber = c.ClaimNumber,
        Status = c.Status.ToString(),
        ReportedProblem = c.ReportedProblem,
        InspectionNotes = c.InspectionNotes,
        DecisionReason = c.DecisionReason,
        Notes = c.Notes,
        DefectiveSerial = c.DefectiveSerial,
        ReplacementProductId = c.ReplacementProductId,
        ReplacementProductName = c.ReplacementProductName,
        ReplacementSerial = c.ReplacementSerial,
        ReplacementStockMoved = c.ReplacementStockMoved,
        SubmittedOn = c.SubmittedOn,
        ResolvedOn = c.ResolvedOn,
        Events = c.Events
            .OrderBy(e => e.Id)
            .Select(e => new WarrantyClaimEventDto
            {
                Status = e.Status.ToString(),
                Notes = e.Notes,
                At = e.CreatedOn,
                By = e.CreatedBy
            })
            .ToList()
    };
}
