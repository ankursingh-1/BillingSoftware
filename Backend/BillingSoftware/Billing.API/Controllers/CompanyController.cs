using Billing.API.Services;
using Billing.Application.DTOs.Company;
using Billing.Shared.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Billing.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CompanyController : ControllerBase
{
    private readonly CompanyService _companyService;
    private readonly CompanyLogoStore _logos;

    public CompanyController(CompanyService companyService, CompanyLogoStore logos)
    {
        _companyService = companyService;
        _logos = logos;
    }

    [Authorize]
    [HttpGet]
    public async Task<IActionResult> Get()
    {
        var company = await _companyService.GetAsync();

        if (company == null)
            return NotFound("Company settings not found.");

        return Ok(company);
    }

    [Authorize(Roles = AppRoles.Admin)]
    [HttpPost]
    public async Task<IActionResult> Save(SaveCompanyRequest request)
    {
        return Ok(await _companyService.SaveAsync(request));
    }

    /// <summary>Uploads or replaces the logo (PNG or JPEG, up to 2 MB).</summary>
    [Authorize(Roles = AppRoles.Admin)]
    [HttpPost("logo")]
    [RequestSizeLimit(CompanyLogoStore.MaxBytes + 64 * 1024)]
    public async Task<IActionResult> UploadLogo(IFormFile file)
    {
        var storedName = await _logos.SaveAsync(file);

        try
        {
            await _companyService.SetLogoAsync(storedName);
        }
        catch
        {
            _logos.Delete(storedName);
            throw;
        }

        // The previous logo file is kept: issued invoices keep the logo they were created with.
        return Ok(new { Success = true, Message = "Logo saved." });
    }

    [Authorize(Roles = AppRoles.Admin)]
    [HttpDelete("logo")]
    public async Task<IActionResult> RemoveLogo()
    {
        await _companyService.SetLogoAsync(null);

        return Ok(new { Success = true, Message = "Logo removed." });
    }

    /// <summary>The current logo. Public so it can be used in an img tag and on printed pages.</summary>
    [AllowAnonymous]
    [HttpGet("logo")]
    public async Task<IActionResult> GetLogo()
    {
        var logo = await _logos.ReadCurrentAsync();

        return logo == null ? NotFound() : File(logo.Value.Bytes, logo.Value.ContentType);
    }

    /// <summary>A specific stored logo (used by invoices issued with an earlier logo).</summary>
    [AllowAnonymous]
    [HttpGet("logo/{fileName}")]
    public async Task<IActionResult> GetLogoByName(string fileName)
    {
        var logo = await _logos.ReadAsync(fileName);

        if (logo == null)
            return NotFound();

        Response.Headers.CacheControl = "public, max-age=86400";
        return File(logo.Value.Bytes, logo.Value.ContentType);
    }
}