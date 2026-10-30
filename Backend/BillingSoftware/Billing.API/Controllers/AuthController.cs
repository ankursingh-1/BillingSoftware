using System.Security.Claims;
using Billing.Infrastructure.Security;
using Billing.Persistence.Context;
using Billing.Shared.Exceptions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
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

    [EnableRateLimiting("login")]
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

    public class ChangePasswordModel
    {
        public string CurrentPassword { get; set; } = string.Empty;
        public string NewPassword { get; set; } = string.Empty;
    }

    [Authorize]
    [HttpPost("change-password")]
    public async Task<IActionResult> ChangePassword(ChangePasswordModel model)
    {
        var idValue = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!int.TryParse(idValue, out var userId))
            return Unauthorized();

        if (string.IsNullOrWhiteSpace(model.CurrentPassword) ||
            string.IsNullOrWhiteSpace(model.NewPassword))
            throw new BusinessException("Current and new passwords are required.");

        if (model.NewPassword.Length < 8)
            throw new BusinessException("The new password must be at least 8 characters.");

        if (model.NewPassword == model.CurrentPassword)
            throw new BusinessException("The new password must be different from the current password.");

        var user = await _context.Users
            .FirstOrDefaultAsync(x => x.Id == userId && x.IsActive && !x.IsDeleted)
            ?? throw BusinessException.NotFound("Account not found.");

        if (!_passwordService.Verify(model.CurrentPassword, user.PasswordHash))
            throw new BusinessException("The current password is incorrect.");

        user.PasswordHash = _passwordService.Hash(model.NewPassword);
        user.modifieson = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        return Ok(new { Success = true, Message = "Password changed." });
    }
}