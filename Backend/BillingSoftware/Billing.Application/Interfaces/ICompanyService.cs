using Billing.Application.DTOs.Company;

namespace Billing.Application.Interfaces;

public interface ICompanyService
{
    Task<CompanyDto?> GetAsync();
    Task<CompanyDto> SaveAsync(SaveCompanyRequest request);
}