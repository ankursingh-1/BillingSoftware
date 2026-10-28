using Billing.API.Services;
using Billing.Application.DTOs.StockLedger;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Billing.API.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class StockLedgerController : ControllerBase
    {
        private readonly StockLedgerService _service;

        public StockLedgerController(StockLedgerService service)
        {
            _service = service;
        }

        /// <summary>Paged stock movements with optional product, type, date and text filters.</summary>
        [HttpGet]
        public async Task<IActionResult> Search([FromQuery] StockLedgerSearchRequest request)
        {
            return Ok(await _service.SearchAsync(request));
        }

        [HttpGet("{productId:int}")]
        public async Task<IActionResult> GetByProduct(int productId)
        {
            return Ok(await _service.GetByProductAsync(productId));
        }
    }
}