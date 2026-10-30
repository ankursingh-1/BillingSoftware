using Billing.Domain.Common;
using Billing.Domain.Enums;

namespace Billing.Domain.Entities;

public class Company : BaseEntity
{
    public string CompanyName { get; set; } = string.Empty;
    public string? GSTNumber { get; set; }
    public string? PANNumber { get; set; }
    public string? Address { get; set; }
    public string? City { get; set; }
    public string? State { get; set; }

    /// <summary>Two-digit GST state code of the company (place of supply = this state for intra-state sales).</summary>
    public string? StateCode { get; set; }
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

    // ---- Invoice print settings (presentation only; never change saved invoice data) ----
    /// <summary>
    /// Counter defaults: grocery prefers walk-in; electronics prompts for warranty.
    /// </summary>
    public ShopType ShopType { get; set; } = ShopType.GeneralRetail;

    public InvoiceFormat DefaultInvoiceFormat { get; set; } = InvoiceFormat.A4;

    /// <summary>58 or 80.</summary>
    public int ThermalPaperWidthMm { get; set; } = 80;

    /// <summary>Bank/UPI details are off by default: they are not wanted on counter bills.</summary>
    public bool ShowBankDetails { get; set; }
    public bool ShowLogo { get; set; } = true;
    public bool ShowTerms { get; set; } = true;
    public bool ShowFooter { get; set; } = true;

    public bool IsActive { get; set; } = true;
}