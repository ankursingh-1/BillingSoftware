using Billing.API.Services;
using Billing.Application.DTOs.Inventory;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Billing.API.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class InventoryController : ControllerBase
{
    private readonly InventoryQueryService _service;

    public InventoryController(InventoryQueryService service)
    {
        _service = service;
    }

    [HttpGet("stock")]
    public async Task<IActionResult> Stock([FromQuery] StockSearchRequest request)
    {
        return Ok(await _service.SearchAsync(request));
    }

    [HttpGet("summary")]
    public async Task<IActionResult> Summary()
    {
        return Ok(await _service.GetSummaryAsync());
    }
}