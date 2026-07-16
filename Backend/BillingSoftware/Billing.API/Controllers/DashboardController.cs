using Billing.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Billing.API.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class DashboardController : ControllerBase
{
    private readonly DashboardService _dashboardService;

    public DashboardController(DashboardService dashboardService)
    {
        _dashboardService = dashboardService;
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

    [HttpGet("top-products")]
    public async Task<IActionResult> GetTopProducts()
    {
        var result = await _dashboardService.GetTopSellingProductsAsync();

        return Ok(result);
    }

    [HttpGet("sales-chart")]
    public async Task<IActionResult> GetSalesChart([FromQuery] int days = 7)
    {
        var result = await _dashboardService.GetSalesChartAsync(days);

        return Ok(result);
    }

    [HttpGet("purchase-chart")]
    public async Task<IActionResult> GetPurchaseChart([FromQuery] int days = 7)
    {
        var result = await _dashboardService.GetPurchaseChartAsync(days);

        return Ok(result);
    }
}