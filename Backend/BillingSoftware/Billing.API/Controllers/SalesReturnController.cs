using Billing.Application.DTOs.SalesReturn;
using Billing.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace Billing.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class SalesReturnController : ControllerBase
    {
        private readonly ISalesReturnService _salesReturnService;

        public SalesReturnController(ISalesReturnService salesReturnService)
        {
            _salesReturnService = salesReturnService;
        }

        [HttpPost]
        public async Task<IActionResult> Create(CreateSalesReturnDto dto)
        {
            await _salesReturnService.CreateAsync(dto);

            return Ok(new
            {
                Success = true,
                Message = "Sales Return created successfully."
            });
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var result = await _salesReturnService.GetAllAsync();
            return Ok(result);
        }
    }
}