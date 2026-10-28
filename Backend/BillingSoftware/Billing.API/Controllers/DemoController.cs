using Billing.API.Services;
using Billing.Shared.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Billing.API.Controllers;

[Authorize(Roles = AppRoles.Admin)]
[ApiController]
[Route("api/[controller]")]
public class DemoController : ControllerBase
{
    private readonly DemoDataService _demo;

    public DemoController(DemoDataService demo)
    {
        _demo = demo;
    }

    /// <summary>
    /// Seeds labelled DEMO records. Disabled unless Demo:Enabled=true.
    /// Safe to call repeatedly; existing DEMO rows are reused.
    /// </summary>
    [HttpPost("seed")]
    public async Task<IActionResult> Seed() => Ok(await _demo.SeedAsync());
}
