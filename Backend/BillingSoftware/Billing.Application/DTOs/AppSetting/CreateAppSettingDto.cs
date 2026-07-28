using System.ComponentModel.DataAnnotations;

namespace Billing.Application.DTOs.AppSetting
{
    public class CreateAppSettingDto
    {
        [Required(ErrorMessage = "Company name is required.")]
        [StringLength(200, ErrorMessage = "Company name cannot exceed 200 characters.")]
        public string CompanyName { get; set; } = string.Empty;

        [StringLength(500, ErrorMessage = "Company address cannot exceed 500 characters.")]
        public string? CompanyAddress { get; set; }

        [Phone(ErrorMessage = "Invalid phone number.")]
        public string? Phone { get; set; }

        [EmailAddress(ErrorMessage = "Invalid email address.")]
        public string? Email { get; set; }

        [StringLength(15, ErrorMessage = "GST Number cannot exceed 15 characters.")]
        public string? GSTNumber { get; set; }

        [StringLength(10, ErrorMessage = "Currency cannot exceed 10 characters.")]
        public string? Currency { get; set; }

        [StringLength(10, ErrorMessage = "Invoice Prefix cannot exceed 10 characters.")]
        public string? InvoicePrefix { get; set; }

        [Url(ErrorMessage = "Invalid Logo URL.")]
        public string? LogoUrl { get; set; }
    }
}