using System.ComponentModel.DataAnnotations;

namespace Billing.API.Models;

public class SupplierDto
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Contact name is required.")]
    [StringLength(200, ErrorMessage = "Contact name cannot exceed 200 characters.")]
    public string Name { get; set; } = string.Empty;

    [Required(ErrorMessage = "Company name is required.")]
    [StringLength(200, ErrorMessage = "Company name cannot exceed 200 characters.")]
    public string CompanyName { get; set; } = string.Empty;

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
}
