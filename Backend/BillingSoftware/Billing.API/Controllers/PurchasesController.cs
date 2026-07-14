using Billing.API.Models;
using Billing.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Billing.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class PurchasesController : ControllerBase
{
    private readonly PurchaseService _service;

    public PurchasesController(PurchaseService service)
    {
        _service = service;
    }

    [HttpPost]
    public async Task<IActionResult> Create(CreatePurchaseDto dto)
    {
        var id = await _service.CreateAsync(dto);

        return Ok(new
        {
            Success = true,
            Message = "Purchase created successfully.",
            PurchaseId = id
        });
    }
}