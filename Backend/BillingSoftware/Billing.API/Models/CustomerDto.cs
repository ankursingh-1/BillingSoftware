using System.ComponentModel.DataAnnotations;

namespace Billing.API.Models;

public class CustomerDto
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Name is required.")]
    [StringLength(200, ErrorMessage = "Name cannot exceed 200 characters.")]
    public string Name { get; set; } = string.Empty;

    [Required(ErrorMessage = "Mobile number is required.")]
    [StringLength(15, ErrorMessage = "Mobile number cannot exceed 15 characters.")]
    [RegularExpression(@"^\+?[0-9]{7,15}$",
        ErrorMessage = "Enter a valid mobile number (7 to 15 digits, optional leading +).")]
    public string Mobile { get; set; } = string.Empty;

    [EmailAddress(ErrorMessage = "Enter a valid email address.")]
    [StringLength(200, ErrorMessage = "Email cannot exceed 200 characters.")]
    public string? Email { get; set; }

    [StringLength(500, ErrorMessage = "Address cannot exceed 500 characters.")]
    public string? Address { get; set; }

    /// <summary>State name (official spelling). Needed to choose CGST/SGST vs IGST.</summary>
    [StringLength(100, ErrorMessage = "State cannot exceed 100 characters.")]
    public string? State { get; set; }

    /// <summary>Two-digit GST state code, e.g. 36 for Telangana.</summary>
    [StringLength(2, ErrorMessage = "State code must be 2 digits.")]
    public string? StateCode { get; set; }

    /// <summary>15-character GSTIN for business customers/suppliers.</summary>
    [StringLength(15, ErrorMessage = "GSTIN must be 15 characters.")]
    public string? Gstin { get; set; }
}