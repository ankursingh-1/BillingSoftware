using Billing.API.Services;
using Billing.Application.DTOs.Returns;
using Billing.Shared.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Billing.API.Controllers;

/// <summary>Debit notes against purchases. Purchasing is an administrator function.</summary>
[Authorize(Roles = AppRoles.Admin)]
[ApiController]
[Route("api/[controller]")]
public class PurchaseReturnsController : ControllerBase
{
    private readonly PurchaseReturnService _service;

    public PurchaseReturnsController(PurchaseReturnService service)
    {
        _service = service;
    }

    [HttpPost]
    public async Task<IActionResult> Create(CreatePurchaseReturnRequest request)
    {
        var result = await _service.CreateAsync(request);

        return Ok(new
        {
            Success = true,
            Message = "Purchase return created successfully.",
            result.ReturnId,
            result.ReturnNumber,
            result.TotalAmount,
            result.RefundReceived
        });
    }

    [HttpGet]
    public async Task<IActionResult> Search([FromQuery] PurchaseReturnSearchRequest request)
    {
        return Ok(await _service.SearchAsync(request));
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        return Ok(await _service.GetByIdAsync(id));
    }
}