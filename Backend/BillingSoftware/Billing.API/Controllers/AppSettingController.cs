using Billing.Application.DTOs.AppSetting;
using Billing.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace Billing.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AppSettingController : ControllerBase
    {
        private readonly IAppSettingService _service;

        public AppSettingController(IAppSettingService service)
        {
            _service = service;
        }

        [HttpGet]
        public async Task<IActionResult> Get()
        {
            return Ok(await _service.GetAsync());
        }

        [HttpPost]
        public async Task<IActionResult> Save(CreateAppSettingDto dto)
        {
            await _service.SaveAsync(dto);

            return Ok(new
            {
                Success = true,
                Message = "Settings saved successfully."
            });
        }
    }
}