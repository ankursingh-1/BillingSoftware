using Billing.API.Services;
using Billing.Application.DTOs.Sales;
using Billing.Domain.Enums;
using Billing.Shared.Exceptions;
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
    private readonly ReceiptPdfService _receiptPdf;
    private readonly CompanyLogoStore _logos;

    public SalesController(
        SaleService service, InvoicePdfService pdf, ReceiptPdfService receiptPdf, CompanyLogoStore logos)
    {
        _service = service;
        _pdf = pdf;
        _receiptPdf = receiptPdf;
        _logos = logos;
    }

    private static (InvoiceFormat Format, int Width) ResolveFormat(string? format, int? width, InvoiceSettingsDto defaults)
    {
        InvoiceFormat chosen;

        if (string.IsNullOrWhiteSpace(format))
            Enum.TryParse(defaults.DefaultFormat, true, out chosen);
        else if (!Enum.TryParse(format, true, out chosen) || !Enum.IsDefined(chosen))
            throw new BusinessException("Unknown format. Use A4, Compact or Thermal.");

        if (chosen == 0)
            chosen = InvoiceFormat.A4;

        var w = width ?? defaults.ThermalPaperWidthMm;
        if (w != 58 && w != 80)
            throw new BusinessException("Thermal paper width must be 58 or 80 (millimetres).");

        return (chosen, w);
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
    public async Task<IActionResult> Pdf(int id, [FromQuery] string? format, [FromQuery] int? width)
    {
        var sale = await _service.GetByIdAsync(id);
        var (chosen, thermalWidth) = ResolveFormat(format, width, sale.Settings);

        var logo = sale.Company.HasLogo ? await _logos.ReadAsync(sale.Company.LogoFile) : null;
        var bytes = _pdf.Generate(sale, chosen, thermalWidth, logo?.Bytes);

        return File(bytes, "application/pdf", $"{sale.InvoiceNumber}-{chosen}.pdf");
    }

    [HttpGet("{id:int}/payments/{paymentId:int}/receipt")]
    public async Task<IActionResult> Receipt(int id, int paymentId)
    {
        return Ok(await _service.GetReceiptAsync(id, paymentId));
    }

    [HttpGet("{id:int}/payments/{paymentId:int}/receipt/pdf")]
    public async Task<IActionResult> ReceiptPdf(
        int id, int paymentId, [FromQuery] string? format, [FromQuery] int? width)
    {
        var receipt = await _service.GetReceiptAsync(id, paymentId);
        var (chosen, thermalWidth) = ResolveFormat(format, width, receipt.Settings);

        // A receipt is either a standard A5 slip or a thermal roll; "Compact" means the standard slip.
        var logo = receipt.Company.HasLogo ? await _logos.ReadAsync(receipt.Company.LogoFile) : null;
        var bytes = _receiptPdf.Generate(receipt, chosen, thermalWidth, logo?.Bytes);

        return File(bytes, "application/pdf", $"{receipt.ReceiptNumber}.pdf");
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
