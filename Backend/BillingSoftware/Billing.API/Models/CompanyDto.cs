namespace Billing.API.Models;

public class CompanyDto
{
    public string CompanyName { get; set; } = string.Empty;
    public string? GSTNumber { get; set; }
    public string? Address { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string? Website { get; set; }
    public string? LogoPath { get; set; }
    public string InvoicePrefix { get; set; } = "INV";
    public string Currency { get; set; } = "INR";
}