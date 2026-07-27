using Billing.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace Billing.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class StockLedgerController : ControllerBase
    {
        private readonly IStockLedgerService _stockLedgerService;

        public StockLedgerController(IStockLedgerService stockLedgerService)
        {
            _stockLedgerService = stockLedgerService;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var result = await _stockLedgerService.GetAllAsync();

            return Ok(result);
        }

        [HttpGet("{productId:int}")]
        public async Task<IActionResult> GetByProduct(int productId)
        {
            var result = await _stockLedgerService.GetByProductAsync(productId);

            return Ok(result);
        }
    }
}