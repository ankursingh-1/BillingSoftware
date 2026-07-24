using Billing.Domain.Common;

namespace Billing.Domain.Entities;

public class Company : BaseEntity
{
    public string CompanyName { get; set; } = string.Empty;
    public string? GSTNumber { get; set; }
    public string? PANNumber { get; set; }
    public string? Address { get; set; }
    public string? City { get; set; }
    public string? State { get; set; }
    public string? Country { get; set; }
    public string? Pincode { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string? Website { get; set; }
    public string? LogoPath { get; set; }
    public string InvoicePrefix { get; set; } = "INV";
    public string Currency { get; set; } = "INR";
    public string? BankName { get; set; }
    public string? AccountNumber { get; set; }
    public string? IFSCCode { get; set; }
    public string? UPIId { get; set; }
    public string? TermsAndConditions { get; set; }
    public string? FooterMessage { get; set; }
    public bool IsActive { get; set; } = true;
}