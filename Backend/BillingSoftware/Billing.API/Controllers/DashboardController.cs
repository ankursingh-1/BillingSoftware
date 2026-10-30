using Billing.API.Services;
using Billing.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Billing.API.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class DashboardController : ControllerBase
{
    private const int MaxChartDays = 90;

    private readonly DashboardService _dashboardService;
    private readonly IWarrantyService _warranties;

    public DashboardController(DashboardService dashboardService, IWarrantyService warranties)
    {
        _dashboardService = dashboardService;
        _warranties = warranties;
    }

    [HttpGet("summary")]
    public async Task<IActionResult> GetSummary()
    {
        var result = await _dashboardService.GetSummaryAsync();

        return Ok(result);
    }

    [HttpGet("recent-sales")]
    public async Task<IActionResult> GetRecentSales()
    {
        var result = await _dashboardService.GetRecentSalesAsync();

        return Ok(result);
    }

    [HttpGet("recent-purchases")]
    public async Task<IActionResult> GetRecentPurchases()
    {
        var result = await _dashboardService.GetRecentPurchasesAsync();

        return Ok(result);
    }

    [HttpGet("low-stock")]
    public async Task<IActionResult> GetLowStock()
    {
        var result = await _dashboardService.GetLowStockProductsAsync();

        return Ok(result);
    }

    [HttpGet("expiring-warranties")]
    public async Task<IActionResult> GetExpiringWarranties()
    {
        return Ok(await _warranties.ExpiringAsync(8));
    }

    [HttpGet("top-products")]
    public async Task<IActionResult> GetTopProducts()
    {
        var result = await _dashboardService.GetTopSellingProductsAsync();

        return Ok(result);
    }

    [HttpGet("sales-chart")]
    public async Task<IActionResult> GetSalesChart([FromQuery] int days = 7)
    {
        if (days < 1 || days > MaxChartDays)
            return BadRequest($"days must be between 1 and {MaxChartDays}.");

        var result = await _dashboardService.GetSalesChartAsync(days);

        return Ok(result);
    }

    [HttpGet("purchase-chart")]
    public async Task<IActionResult> GetPurchaseChart([FromQuery] int days = 7)
    {
        if (days < 1 || days > MaxChartDays)
            return BadRequest($"days must be between 1 and {MaxChartDays}.");

        var result = await _dashboardService.GetPurchaseChartAsync(days);

        return Ok(result);
    }
}