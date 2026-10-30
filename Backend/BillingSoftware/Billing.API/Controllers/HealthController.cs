using Billing.Shared.Responses;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Billing.API.Controllers
{
    [AllowAnonymous]
    [ApiController]
    [Route("api/[controller]")]
    public class HealthController : ControllerBase
    {

        [HttpGet]
        public IActionResult Get()
        {
            var response = new ApiResponse<object>
            {
                Success = true,
                Message = "Billing API is running",
                Data = new
                {
                    Version = "1.0.0",
                    ServerTime = DateTime.UtcNow
                }
            };
            return Ok(response);
        }
    }
}
