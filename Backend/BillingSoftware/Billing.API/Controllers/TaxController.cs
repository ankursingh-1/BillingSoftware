using Microsoft.AspNetCore.Authorization;
using Billing.Application.DTOs.Tax;
using Billing.Application.Interfaces;
using Billing.Shared.Security;
using Microsoft.AspNetCore.Mvc;

namespace Billing.API.Controllers
{
    [Route("api/[controller]")]
    [Authorize]
    [ApiController]
    public class TaxController : ControllerBase
    {
        private readonly ITaxService _taxService;

        public TaxController(ITaxService taxService)
        {
            _taxService = taxService;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var result = await _taxService.GetAllAsync();
            return Ok(result);
        }

        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetById(int id)
        {
            var result = await _taxService.GetByIdAsync(id);

            if (result == null)
                return NotFound();

            return Ok(result);
        }

        [Authorize(Roles = AppRoles.Admin)]
        [HttpPost]
        public async Task<IActionResult> Create(SaveTaxRequest request)
        {
            var result = await _taxService.CreateAsync(request);
            return Ok(result);
        }

        [Authorize(Roles = AppRoles.Admin)]
        [HttpPut("{id:int}")]
        public async Task<IActionResult> Update(int id, SaveTaxRequest request)
        {
            var result = await _taxService.UpdateAsync(id, request);

            if (result == null)
                return NotFound();

            return Ok(result);
        }

        [Authorize(Roles = AppRoles.Admin)]
        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id)
        {
            await _taxService.DeleteAsync(id);
            return Ok(new { Message = "Tax deleted successfully." });
        }
    }
}