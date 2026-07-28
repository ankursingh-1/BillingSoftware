using Billing.Application.DTOs.PurchaseReturn;
using Billing.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace Billing.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PurchaseReturnController : ControllerBase
    {
        private readonly IPurchaseReturnService _purchaseReturnService;

        public PurchaseReturnController(IPurchaseReturnService purchaseReturnService)
        {
            _purchaseReturnService = purchaseReturnService;
        }

        [HttpPost]
        public async Task<IActionResult> Create(CreatePurchaseReturnDto dto)
        {
            await _purchaseReturnService.CreateAsync(dto);

            return Ok(new
            {
                Success = true,
                Message = "Purchase Return created successfully."
            });
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var result = await _purchaseReturnService.GetAllAsync();
            return Ok(result);
        }
    }
}