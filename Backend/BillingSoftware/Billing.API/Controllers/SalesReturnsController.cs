using Billing.API.Services;
using Billing.Application.DTOs.Returns;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Billing.API.Controllers;

/// <summary>Credit notes against sales invoices (billing staff and administrators).</summary>
[Authorize]
[ApiController]
[Route("api/[controller]")]
public class SalesReturnsController : ControllerBase
{
    private readonly SalesReturnService _service;

    public SalesReturnsController(SalesReturnService service)
    {
        _service = service;
    }

    [HttpPost]
    public async Task<IActionResult> Create(CreateSalesReturnRequest request)
    {
        var result = await _service.CreateAsync(request);

        return Ok(new
        {
            Success = true,
            Message = "Sales return created successfully.",
            result.ReturnId,
            result.ReturnNumber,
            result.TotalAmount,
            result.RefundAmount,
            result.RefundDue,
            result.Outstanding
        });
    }

    [HttpPost("{id:int}/refund")]
    public async Task<IActionResult> Refund(int id, RecordRefundRequest request)
    {
        return Ok(await _service.RecordRefundAsync(id, request));
    }

    [HttpGet]
    public async Task<IActionResult> Search([FromQuery] SalesReturnSearchRequest request)
    {
        return Ok(await _service.SearchAsync(request));
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        return Ok(await _service.GetByIdAsync(id));
    }
}