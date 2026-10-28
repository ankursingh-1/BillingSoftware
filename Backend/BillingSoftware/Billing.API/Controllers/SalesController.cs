using Billing.API.Services;
using Billing.Application.DTOs.Sales;
using Billing.Shared.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Billing.API.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class SalesController : ControllerBase
{
    private readonly SaleService _service;
    private readonly InvoicePdfService _pdf;
    private readonly CompanyLogoStore _logos;

    public SalesController(SaleService service, InvoicePdfService pdf, CompanyLogoStore logos)
    {
        _service = service;
        _pdf = pdf;
        _logos = logos;
    }

    [HttpPost]
    public async Task<IActionResult> Create(CreateSaleRequest request)
    {
        var sale = await _service.CreateAsync(request);

        return Ok(new
        {
            Message = sale.IsDuplicate
                ? "This sale was already created."
                : "Sale created successfully.",
            sale.SaleId,
            sale.InvoiceNumber,
            sale.TotalAmount,
            sale.PaidAmount,
            sale.Outstanding,
            sale.PaymentStatus,
            sale.IsDuplicate
        });
    }

    [HttpGet]
    public async Task<IActionResult> Search([FromQuery] SaleSearchRequest request)
    {
        return Ok(await _service.SearchAsync(request));
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        return Ok(await _service.GetByIdAsync(id));
    }

    [HttpGet("{id:int}/pdf")]
    public async Task<IActionResult> Pdf(int id)
    {
        var sale = await _service.GetByIdAsync(id);

        var logo = sale.Company.HasLogo ? await _logos.ReadCurrentAsync() : null;
        var bytes = _pdf.Generate(sale, logo?.Bytes);

        return File(bytes, "application/pdf", $"{sale.InvoiceNumber}.pdf");
    }

    [HttpPost("{id:int}/payments")]
    public async Task<IActionResult> RecordPayment(int id, RecordPaymentRequest request)
    {
        return Ok(await _service.RecordPaymentAsync(id, request));
    }

    [Authorize(Roles = AppRoles.Admin)]
    [HttpPost("{id:int}/payments/{paymentId:int}/void")]
    public async Task<IActionResult> VoidPayment(int id, int paymentId, VoidPaymentRequest request)
    {
        return Ok(await _service.VoidPaymentAsync(id, paymentId, request));
    }
}
