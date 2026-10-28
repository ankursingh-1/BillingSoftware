using Billing.API.Services;
using Billing.Application.DTOs.Purchases;
using Billing.Shared.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Billing.API.Controllers;

/// <summary>Purchases change stock and cost, so they are an administrator function.</summary>
[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = AppRoles.Admin)]
public class PurchasesController : ControllerBase
{
    private readonly PurchaseService _service;

    public PurchasesController(PurchaseService service)
    {
        _service = service;
    }

    [HttpPost]
    public async Task<IActionResult> Create(CreatePurchaseRequest request)
    {
        var purchase = await _service.CreateAsync(request);

        return Ok(new
        {
            Success = true,
            Message = purchase.IsDuplicate
                ? "This purchase was already created."
                : "Purchase created successfully.",
            purchase.PurchaseId,
            purchase.PurchaseNumber,
            purchase.TotalAmount,
            purchase.IsDuplicate
        });
    }

    [HttpGet]
    public async Task<IActionResult> Search([FromQuery] PurchaseSearchRequest request)
    {
        return Ok(await _service.SearchAsync(request));
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        return Ok(await _service.GetByIdAsync(id));
    }
}