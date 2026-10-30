namespace Billing.Application.DTOs.Company;

public class CompanyDto
{
    public int Id { get; set; }
    public string CompanyName { get; set; } = string.Empty;
    public string? GSTNumber { get; set; }
    public string? PANNumber { get; set; }
    public string? Address { get; set; }
    public string? City { get; set; }
    public string? State { get; set; }
    public string? StateCode { get; set; }
    public string? Country { get; set; }
    public string? Pincode { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string? Website { get; set; }

    /// <summary>True when a logo has been uploaded. The image is served from /api/Company/logo.</summary>
    public bool HasLogo { get; set; }
    public string InvoicePrefix { get; set; } = "INV";
    public string Currency { get; set; } = "INR";
    public string? BankName { get; set; }
    public string? AccountNumber { get; set; }
    public string? IFSCCode { get; set; }
    public string? UPIId { get; set; }
    public string? TermsAndConditions { get; set; }
    public string? FooterMessage { get; set; }

    // Invoice print settings
    public string DefaultInvoiceFormat { get; set; } = "A4";
    public int ThermalPaperWidthMm { get; set; } = 80;
    public bool ShowBankDetails { get; set; }
    public bool ShowLogo { get; set; } = true;
    public bool ShowTerms { get; set; } = true;
    public bool ShowFooter { get; set; } = true;

    public bool IsActive { get; set; }

    /// <summary>GeneralRetail, Electronics or Grocery.</summary>
    public string ShopType { get; set; } = "GeneralRetail";
}