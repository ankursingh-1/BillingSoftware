using Billing.Application.DTOs.StockAdjustment;
using Billing.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace Billing.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class StockAdjustmentController : ControllerBase
    {
        private readonly IStockAdjustmentService _stockAdjustmentService;

        public StockAdjustmentController(IStockAdjustmentService stockAdjustmentService)
        {
            _stockAdjustmentService = stockAdjustmentService;
        }

        [HttpPost]
        public async Task<IActionResult> AdjustStock(CreateStockAdjustmentDto dto)
        {
            await _stockAdjustmentService.AdjustStockAsync(dto);

            return Ok(new
            {
                Success = true,
                Message = "Stock adjusted successfully."
            });
        }

        [HttpGet]
        public async Task<IActionResult> GetHistory()
        {
            var result = await _stockAdjustmentService.GetHistoryAsync();

            return Ok(result);
        }
    }
}