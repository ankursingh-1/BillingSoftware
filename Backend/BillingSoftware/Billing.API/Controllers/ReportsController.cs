using Billing.API.Services;
using Billing.Application.DTOs.Reports;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;

namespace Billing.API.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class ReportsController : ControllerBase
{
    private readonly ReportService _reportService;

    public ReportsController(ReportService reportService)
    {
        _reportService = reportService;
    }

    [HttpPost("sales")]
    public async Task<IActionResult> GetSalesReport([FromBody] SalesReportRequest request)
    {
        var result = await _reportService.GetSalesReportAsync(request);

        return Ok(result);
    }

    [HttpPost("purchases")]
    public async Task<IActionResult> GetPurchaseReport([FromBody] PurchaseReportRequest request)
    {
        var result = await _reportService.GetPurchaseReportAsync(request);

        return Ok(result);
    }

    [HttpPost("stock")] 
    public async Task<IActionResult> GetStockReport([FromBody] StockReportRequest request)
    {
        var result = await _reportService.GetStockReportAsync(request);

        return Ok(result);
    }

    [HttpPost("profit")]
    public async Task<IActionResult> GetProfitReport([FromBody] ProfitReportRequest request)
    {
        var result = await _reportService.GetProfitReportAsync(request);

        return Ok(result);
    }

    
}