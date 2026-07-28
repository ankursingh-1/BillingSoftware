using System.ComponentModel.DataAnnotations;

namespace Billing.Application.DTOs.Unit;

public class SaveUnitRequest
{
    [Required(ErrorMessage = "Unit name is required.")]
    [StringLength(100, ErrorMessage = "Unit name cannot exceed 100 characters.")]
    public string Name { get; set; } = string.Empty;

    [StringLength(20, ErrorMessage = "Short name cannot exceed 20 characters.")]
    public string? ShortName { get; set; }

    [StringLength(500, ErrorMessage = "Description cannot exceed 500 characters.")]
    public string? Description { get; set; }

    public bool IsActive { get; set; } = true;
}