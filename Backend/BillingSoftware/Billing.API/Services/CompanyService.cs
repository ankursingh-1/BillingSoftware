using System.Text.RegularExpressions;
using Billing.Application.Accounting;
using Billing.Application.DTOs.Company;
using Billing.Application.Interfaces;
using Billing.Domain.Entities;
using Billing.Domain.Enums;
using Billing.Persistence.Context;
using Billing.Shared.Exceptions;
using Microsoft.EntityFrameworkCore;

namespace Billing.API.Services;

public class CompanyService : ICompanyService
{
    private static readonly Regex Pan = new("^[A-Z]{5}[0-9]{4}[A-Z]$", RegexOptions.Compiled);
    private static readonly Regex Ifsc = new("^[A-Z]{4}0[A-Z0-9]{6}$", RegexOptions.Compiled);
    private static readonly Regex Pincode = new("^[0-9]{6}$", RegexOptions.Compiled);
    private static readonly Regex Upi = new(@"^[A-Za-z0-9._\-]{2,}@[A-Za-z0-9]{2,}$", RegexOptions.Compiled);
    private static readonly Regex AccountNumber = new("^[0-9]{6,20}$", RegexOptions.Compiled);
    private static readonly Regex CurrencyCode = new("^[A-Z]{3}$", RegexOptions.Compiled);

    private readonly BillingDbContext _context;
    private readonly IAuditService _audit;

    public CompanyService(BillingDbContext context, IAuditService audit)
    {
        _context = context;
        _audit = audit;
    }

    public async Task<CompanyDto?> GetAsync()
    {
        var company = await _context.Companies.AsNoTracking().OrderBy(c => c.Id).FirstOrDefaultAsync();

        return company == null ? null : ToDto(company);
    }

    public async Task<CompanyDto> SaveAsync(SaveCompanyRequest request)
    {
        var company = await _context.Companies.OrderBy(c => c.Id).FirstOrDefaultAsync();
        var isNew = company == null;

        if (company == null)
        {
            company = new Company();
            _context.Companies.Add(company);
        }

        var gstin = NormalizeOptional(request.GSTNumber)?.ToUpperInvariant();
        if (gstin != null && !GstStates.IsValidGstinFormat(gstin))
            throw new BusinessException("Enter a valid 15-character GSTIN (for example 36ABCDE1234F1Z5).");

        var pan = NormalizeOptional(request.PANNumber)?.ToUpperInvariant();
        if (pan != null && !Pan.IsMatch(pan))
            throw new BusinessException("Enter a valid 10-character PAN (for example ABCDE1234F).");

        var prefix = DocumentNumbering.NormalizePrefix(request.InvoicePrefix)
            ?? throw new BusinessException(
                "The invoice prefix must be 1 to 4 letters or digits (invoice numbers are limited to 16 characters).");

        var currency = request.Currency.Trim().ToUpperInvariant();
        if (!CurrencyCode.IsMatch(currency))
            throw new BusinessException("Currency must be a 3-letter code such as INR.");

        var pincode = NormalizeOptional(request.Pincode);
        if (pincode != null && !Pincode.IsMatch(pincode))
            throw new BusinessException("Enter a valid 6-digit pincode.");

        var ifsc = NormalizeOptional(request.IFSCCode)?.ToUpperInvariant();
        if (ifsc != null && !Ifsc.IsMatch(ifsc))
            throw new BusinessException("Enter a valid IFSC code (for example HDFC0001234).");

        var account = NormalizeOptional(request.AccountNumber);
        if (account != null && !AccountNumber.IsMatch(account))
            throw new BusinessException("The account number must be 6 to 20 digits.");

        var upi = NormalizeOptional(request.UPIId);
        if (upi != null && !Upi.IsMatch(upi))
            throw new BusinessException("Enter a valid UPI ID (for example shop@bank).");

        if (!Enum.TryParse<InvoiceFormat>(request.DefaultInvoiceFormat, true, out var format)
            || !Enum.IsDefined(format))
            throw new BusinessException("The default invoice format must be A4, Compact or Thermal.");

        if (request.ThermalPaperWidthMm != 58 && request.ThermalPaperWidthMm != 80)
            throw new BusinessException("Thermal paper width must be 58 or 80 mm.");

        // ---- State: code wins; otherwise derive it from the GSTIN or the state name ----
        var stateCode = NormalizeOptional(request.StateCode);
        if (stateCode != null && !GstStates.IsValidCode(stateCode))
            throw new BusinessException("Select a valid state.");

        stateCode ??= GstStates.CodeFromName(request.State);
        var gstinState = GstStates.CodeFromGstin(gstin);

        if (stateCode != null && gstinState != null && stateCode != gstinState)
        {
            throw new BusinessException(
                $"The GSTIN starts with {gstinState} ({GstStates.NameOf(gstinState)}) but the selected state is " +
                $"{GstStates.NameOf(stateCode)} ({stateCode}). Correct one of them.");
        }

        stateCode ??= gstinState;

        if (stateCode == null && !string.IsNullOrWhiteSpace(request.State))
            throw new BusinessException("Select the state from the list.");

        // Changing the state or GSTIN affects CGST/SGST vs IGST for NEW invoices only;
        // issued invoices keep their own frozen values.
        company.CompanyName = request.CompanyName.Trim();
        company.GSTNumber = gstin;
        company.PANNumber = pan;
        company.Address = NormalizeOptional(request.Address);
        company.City = NormalizeOptional(request.City);
        company.StateCode = stateCode;
        company.State = stateCode == null ? null : GstStates.NameOf(stateCode);
        company.Country = NormalizeOptional(request.Country);
        company.Pincode = pincode;
        company.Phone = NormalizeOptional(request.Phone);
        company.Email = NormalizeOptional(request.Email);
        company.Website = NormalizeOptional(request.Website);
        // LogoPath is deliberately NOT taken from the request; see CompanyLogoStore.
        company.InvoicePrefix = prefix;
        company.Currency = currency;
        company.BankName = NormalizeOptional(request.BankName);
        company.AccountNumber = account;
        company.IFSCCode = ifsc;
        company.UPIId = upi;
        company.TermsAndConditions = NormalizeOptional(request.TermsAndConditions);
        company.FooterMessage = NormalizeOptional(request.FooterMessage);
        company.DefaultInvoiceFormat = format;
        company.ThermalPaperWidthMm = request.ThermalPaperWidthMm;
        company.ShowBankDetails = request.ShowBankDetails;
        company.ShowLogo = request.ShowLogo;
        company.ShowTerms = request.ShowTerms;
        company.ShowFooter = request.ShowFooter;
        company.IsActive = request.IsActive;

        _audit.Record(
            isNew ? "Company.Create" : "Company.Update", "Company", company.Id.ToString(),
            $"{company.CompanyName}; GSTIN {company.GSTNumber ?? "-"}; state {company.StateCode ?? "-"}; " +
            $"prefix {company.InvoicePrefix}; format {format}");

        await _context.SaveChangesAsync();

        return ToDto(company);
    }

    /// <summary>Stores the stored file name of a new logo (or null to remove it). Returns the previous name.</summary>
    public async Task<string?> SetLogoAsync(string? storedName)
    {
        var company = await _context.Companies.OrderBy(c => c.Id).FirstOrDefaultAsync()
            ?? throw BusinessException.Conflict(
                "Save your company details first, then upload the logo.");

        var previous = company.LogoPath;
        company.LogoPath = storedName;

        _audit.Record(
            storedName == null ? "Company.LogoRemove" : "Company.LogoUpload", "Company", company.Id.ToString(),
            storedName == null ? "Logo removed" : "Logo replaced");

        await _context.SaveChangesAsync();

        return previous;
    }

    private static string? NormalizeOptional(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static CompanyDto ToDto(Company company) => new()
    {
        Id = company.Id,
        CompanyName = company.CompanyName,
        GSTNumber = company.GSTNumber,
        PANNumber = company.PANNumber,
        Address = company.Address,
        City = company.City,
        State = company.State,
        StateCode = company.StateCode,
        Country = company.Country,
        Pincode = company.Pincode,
        Phone = company.Phone,
        Email = company.Email,
        Website = company.Website,
        HasLogo = !string.IsNullOrWhiteSpace(company.LogoPath),
        InvoicePrefix = company.InvoicePrefix,
        Currency = company.Currency,
        BankName = company.BankName,
        AccountNumber = company.AccountNumber,
        IFSCCode = company.IFSCCode,
        UPIId = company.UPIId,
        TermsAndConditions = company.TermsAndConditions,
        FooterMessage = company.FooterMessage,
        DefaultInvoiceFormat = company.DefaultInvoiceFormat.ToString(),
        ThermalPaperWidthMm = company.ThermalPaperWidthMm,
        ShowBankDetails = company.ShowBankDetails,
        ShowLogo = company.ShowLogo,
        ShowTerms = company.ShowTerms,
        ShowFooter = company.ShowFooter,
        IsActive = company.IsActive
    };
}