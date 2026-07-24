using Billing.Application.DTOs.Company;
using Billing.Application.Interfaces;
using Billing.Domain.Entities;
using Billing.Persistence.Context;
using Microsoft.EntityFrameworkCore;

namespace Billing.API.Services;

public class CompanyService : ICompanyService
{
    private readonly BillingDbContext _context;

    public CompanyService(BillingDbContext context)
    {
        _context = context;
    }

    public async Task<CompanyDto?> GetAsync()
    {
        var company = await _context.Companies.FirstOrDefaultAsync();

        if (company == null)
            return null;

        return new CompanyDto
        {
            Id = company.Id,
            CompanyName = company.CompanyName,
            GSTNumber = company.GSTNumber,
            PANNumber = company.PANNumber,
            Address = company.Address,
            City = company.City,
            State = company.State,
            Country = company.Country,
            Pincode = company.Pincode,
            Phone = company.Phone,
            Email = company.Email,
            Website = company.Website,
            LogoPath = company.LogoPath,
            InvoicePrefix = company.InvoicePrefix,
            Currency = company.Currency,
            BankName = company.BankName,
            AccountNumber = company.AccountNumber,
            IFSCCode = company.IFSCCode,
            UPIId = company.UPIId,
            TermsAndConditions = company.TermsAndConditions,
            FooterMessage = company.FooterMessage,
            IsActive = company.IsActive
        };
    }

    public async Task<CompanyDto> SaveAsync(SaveCompanyRequest request)
    {
        var company = await _context.Companies.FirstOrDefaultAsync();

        if (company == null)
        {
            company = new Company();
            _context.Companies.Add(company);
        }

        company.CompanyName = request.CompanyName;
        company.GSTNumber = request.GSTNumber;
        company.PANNumber = request.PANNumber;
        company.Address = request.Address;
        company.City = request.City;
        company.State = request.State;
        company.Country = request.Country;
        company.Pincode = request.Pincode;
        company.Phone = request.Phone;
        company.Email = request.Email;
        company.Website = request.Website;
        company.LogoPath = request.LogoPath;
        company.InvoicePrefix = request.InvoicePrefix;
        company.Currency = request.Currency;
        company.BankName = request.BankName;
        company.AccountNumber = request.AccountNumber;
        company.IFSCCode = request.IFSCCode;
        company.UPIId = request.UPIId;
        company.TermsAndConditions = request.TermsAndConditions;
        company.FooterMessage = request.FooterMessage;
        company.IsActive = request.IsActive;

        await _context.SaveChangesAsync();

        return await GetAsync() ?? throw new Exception("Company settings could not be loaded.");
    }
}