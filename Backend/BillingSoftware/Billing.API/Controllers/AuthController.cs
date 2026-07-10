using Billing.Infrastructure.Security;
using Billing.Persistence.Context;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Billing.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly BillingDbContext _context;
    private readonly TokenService _tokenService;
    private readonly PasswordService _passwordService;

    public AuthController(
        BillingDbContext context,
        TokenService tokenService,
        PasswordService passwordService)
    {
        _context = context;
        _tokenService = tokenService;
        _passwordService = passwordService;
    }

    public class LoginModel
    {
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login(LoginModel model)
    {
        var user = await _context.Users
            .Include(x => x.Role)
            .FirstOrDefaultAsync(x =>
                x.Email == model.Email &&
                x.IsActive &&
                !x.IsDeleted);

        if (user == null)
        {
            return Unauthorized(new
            {
                Success = false,
                Message = "Invalid Email or Password"
            });
        }

        var isValid = _passwordService.Verify(
            model.Password,
            user.PasswordHash);

        if (!isValid)
        {
            return Unauthorized(new
            {
                Success = false,
                Message = "Invalid Email or Password"
            });
        }

        var token = _tokenService.GenerateToken(
            user.Id,
            user.Email,
            user.Role.Name);

        return Ok(new
        {
            Success = true,
            Token = token
        });
    }
}