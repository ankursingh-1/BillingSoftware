using System.ComponentModel.DataAnnotations;

namespace Billing.Application.DTOs.Brand;

public class SaveBrandRequest
{
    [Required(ErrorMessage = "Brand name is required.")]
    [StringLength(100, ErrorMessage = "Brand name cannot exceed 100 characters.")]
    public string Name { get; set; } = string.Empty;

    [StringLength(500, ErrorMessage = "Description cannot exceed 500 characters.")]
    public string? Description { get; set; }
    public bool IsActive { get; set; } = true;
}