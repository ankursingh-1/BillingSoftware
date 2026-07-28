using System.ComponentModel.DataAnnotations;

namespace Billing.Application.DTOs.Tax
{
    public class SaveTaxRequest
    {
        [Required(ErrorMessage = "Tax name is required.")]
        [StringLength(100, ErrorMessage = "Tax name cannot exceed 100 characters.")]
        public string Name { get; set; } = string.Empty;

        [Range(0, 100, ErrorMessage = "Tax percentage must be between 0 and 100.")]
        public decimal Percentage { get; set; }

        [StringLength(500, ErrorMessage = "Description cannot exceed 500 characters.")]
        public string? Description { get; set; }

        public bool IsActive { get; set; } = true;
    }
}