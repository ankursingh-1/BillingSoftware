using Billing.Application.DTOs.Warranty;
using Billing.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Billing.API.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class WarrantyController : ControllerBase
{
    private readonly IWarrantyService _service;

    public WarrantyController(IWarrantyService service)
    {
        _service = service;
    }

    [HttpPost("search")]
    public async Task<IActionResult> Search([FromBody] WarrantySearchRequest request) =>
        Ok(await _service.SearchAsync(request));

    [HttpGet("{id:int}")]
    public async Task<IActionResult> Get(int id) =>
        Ok(await _service.GetByIdAsync(id));

    [HttpGet("expiring")]
    public async Task<IActionResult> Expiring([FromQuery] int take = 10) =>
        Ok(await _service.ExpiringAsync(take));

    [HttpPost("reports")]
    public async Task<IActionResult> Reports([FromBody] WarrantyReportRequest request)
    {
        var kind = (request.Kind ?? "active").Trim().ToLowerInvariant();
        if (kind is "claims" or "approved" or "rejected" or "replacements" or "inspection")
            return Ok(await _service.ClaimReportAsync(request));

        return Ok(await _service.ReportAsync(request));
    }

    [HttpPost("{id:int}/claims")]
    public async Task<IActionResult> SubmitClaim(int id, [FromBody] SubmitWarrantyClaimRequest request) =>
        Ok(await _service.SubmitClaimAsync(id, request));

    [HttpPost("claims/{claimId:int}")]
    public async Task<IActionResult> UpdateClaim(int claimId, [FromBody] UpdateWarrantyClaimRequest request) =>
        Ok(await _service.UpdateClaimAsync(claimId, request));
}
