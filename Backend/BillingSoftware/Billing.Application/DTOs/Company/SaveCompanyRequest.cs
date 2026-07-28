using System.ComponentModel.DataAnnotations;

namespace Billing.Application.DTOs.Company;

public class SaveCompanyRequest
{
    [Required(ErrorMessage = "Company name is required.")]
    [StringLength(200, ErrorMessage = "Company name cannot exceed 200 characters.")]
    public string CompanyName { get; set; } = string.Empty;

    [StringLength(15, ErrorMessage = "GST Number cannot exceed 15 characters.")]
    public string? GSTNumber { get; set; }

    [StringLength(10, ErrorMessage = "PAN Number cannot exceed 10 characters.")]
    public string? PANNumber { get; set; }

    [StringLength(500)]
    public string? Address { get; set; }

    [StringLength(100)]
    public string? City { get; set; }

    [StringLength(100)]
    public string? State { get; set; }

    [StringLength(100)]
    public string? Country { get; set; }

    [StringLength(10)]
    public string? Pincode { get; set; }

    [Phone(ErrorMessage = "Invalid phone number.")]
    public string? Phone { get; set; }

    [EmailAddress(ErrorMessage = "Invalid email address.")]
    public string? Email { get; set; }

    [Url(ErrorMessage = "Invalid website URL.")]
    public string? Website { get; set; }

    public string? LogoPath { get; set; }

    [Required]
    [StringLength(10)]
    public string InvoicePrefix { get; set; } = "INV";

    [Required]
    [StringLength(10)]
    public string Currency { get; set; } = "INR";

    [StringLength(200)]
    public string? BankName { get; set; }

    [StringLength(30)]
    public string? AccountNumber { get; set; }

    [StringLength(20)]
    public string? IFSCCode { get; set; }

    [StringLength(100)]
    public string? UPIId { get; set; }

    [StringLength(1000)]
    public string? TermsAndConditions { get; set; }

    [StringLength(500)]
    public string? FooterMessage { get; set; }

    public bool IsActive { get; set; } = true;
}