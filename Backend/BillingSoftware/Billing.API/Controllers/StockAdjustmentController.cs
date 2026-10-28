using Billing.API.Services;
using Billing.Application.DTOs.StockAdjustment;
using Billing.Shared.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Billing.API.Controllers
{
    /// <summary>Changing stock by hand is an administrator function.</summary>
    [Authorize(Roles = AppRoles.Admin)]
    [ApiController]
    [Route("api/[controller]")]
    public class StockAdjustmentController : ControllerBase
    {
        private readonly StockAdjustmentService _service;

        public StockAdjustmentController(StockAdjustmentService service)
        {
            _service = service;
        }

        [HttpPost]
        public async Task<IActionResult> AdjustStock(CreateStockAdjustmentDto dto)
        {
            var result = await _service.AdjustAsync(dto);

            return Ok(new
            {
                Success = true,
                Message = "Stock adjusted successfully.",
                result.ReferenceNo,
                result.ProductId,
                result.PreviousStock,
                result.CurrentStock
            });
        }

        [HttpGet]
        public async Task<IActionResult> GetHistory([FromQuery] StockAdjustmentSearchRequest request)
        {
            return Ok(await _service.GetHistoryAsync(request));
        }
    }
}