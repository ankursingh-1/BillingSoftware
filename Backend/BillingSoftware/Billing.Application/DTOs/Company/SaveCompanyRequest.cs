using System.ComponentModel.DataAnnotations;

namespace Billing.Application.DTOs.Company;

/// <summary>
/// Company profile and invoice settings. The logo is not part of this request: it is
/// uploaded through its own endpoint so the server controls the stored file.
/// </summary>
public class SaveCompanyRequest
{
    [Required(ErrorMessage = "Company name is required.")]
    [StringLength(200, ErrorMessage = "Company name cannot exceed 200 characters.")]
    public string CompanyName { get; set; } = string.Empty;

    [StringLength(15, ErrorMessage = "GST number cannot exceed 15 characters.")]
    public string? GSTNumber { get; set; }

    [StringLength(10, ErrorMessage = "PAN cannot exceed 10 characters.")]
    public string? PANNumber { get; set; }

    [StringLength(500)]
    public string? Address { get; set; }

    [StringLength(100)]
    public string? City { get; set; }

    /// <summary>State name (used with <see cref="StateCode"/>; the code wins when both are sent).</summary>
    [StringLength(100)]
    public string? State { get; set; }

    [StringLength(2, ErrorMessage = "State code must be 2 digits.")]
    public string? StateCode { get; set; }

    [StringLength(100)]
    public string? Country { get; set; }

    [StringLength(10)]
    public string? Pincode { get; set; }

    [StringLength(20, ErrorMessage = "Phone cannot exceed 20 characters.")]
    [Phone(ErrorMessage = "Invalid phone number.")]
    public string? Phone { get; set; }

    [StringLength(150)]
    [EmailAddress(ErrorMessage = "Invalid email address.")]
    public string? Email { get; set; }

    [StringLength(200)]
    [Url(ErrorMessage = "Invalid website URL.")]
    public string? Website { get; set; }

    [Required(ErrorMessage = "Invoice prefix is required.")]
    [StringLength(4, ErrorMessage = "The invoice prefix can have at most 4 characters.")]
    public string InvoicePrefix { get; set; } = "INV";

    [Required]
    [StringLength(3, MinimumLength = 3, ErrorMessage = "Currency must be a 3-letter code such as INR.")]
    public string Currency { get; set; } = "INR";

    [StringLength(150)]
    public string? BankName { get; set; }

    [StringLength(30)]
    public string? AccountNumber { get; set; }

    [StringLength(20)]
    public string? IFSCCode { get; set; }

    [StringLength(100)]
    public string? UPIId { get; set; }

    [StringLength(2000, ErrorMessage = "Terms cannot exceed 2000 characters.")]
    public string? TermsAndConditions { get; set; }

    [StringLength(1000, ErrorMessage = "The footer cannot exceed 1000 characters.")]
    public string? FooterMessage { get; set; }

    /// <summary>A4, Compact or Thermal.</summary>
    [Required(ErrorMessage = "Choose the default invoice format.")]
    public string DefaultInvoiceFormat { get; set; } = "A4";

    [Range(58, 80, ErrorMessage = "Thermal paper width must be 58 or 80 mm.")]
    public int ThermalPaperWidthMm { get; set; } = 80;

    public bool ShowBankDetails { get; set; }
    public bool ShowLogo { get; set; } = true;
    public bool ShowTerms { get; set; } = true;
    public bool ShowFooter { get; set; } = true;

    public bool IsActive { get; set; } = true;
}