using Billing.Application.DTOs.Company;
using Billing.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace Billing.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CompanyController : ControllerBase
{
    private readonly ICompanyService _companyService;

    public CompanyController(ICompanyService companyService)
    {
        _companyService = companyService;
    }

    [HttpGet]
    public async Task<IActionResult> Get()
    {
        var company = await _companyService.GetAsync();

        if (company == null)
            return NotFound("Company settings not found.");

        return Ok(company);
    }

    [HttpPost]
    public async Task<IActionResult> Save(SaveCompanyRequest request)
    {
        var result = await _companyService.SaveAsync(request);

        return Ok(result);
    }
}