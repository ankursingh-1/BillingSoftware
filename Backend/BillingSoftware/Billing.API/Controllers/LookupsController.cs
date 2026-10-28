using Billing.Application.Accounting;
using Billing.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Billing.API.Controllers;

/// <summary>Fixed reference lists used by dropdowns.</summary>
[Authorize]
[ApiController]
[Route("api/[controller]")]
public class LookupsController : ControllerBase
{
    [HttpGet("gst-states")]
    public IActionResult GstStateList() =>
        Ok(GstStates.All.Select(s => new { code = s.Code, name = s.Name }));

    [HttpGet("payment-methods")]
    public IActionResult PaymentMethods() =>
        Ok(Enum.GetValues<PaymentMethod>().Select(m => new { value = m.ToString(), label = Label(m) }));

    private static string Label(PaymentMethod method) => method switch
    {
        PaymentMethod.Upi => "UPI",
        PaymentMethod.BankTransfer => "Bank Transfer",
        _ => method.ToString()
    };
}