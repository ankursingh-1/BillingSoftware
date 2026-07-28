using Billing.Domain.Common;

namespace Billing.Domain.Entities
{
    public class AppSetting : SoftDeleteEntity
    {
        public string CompanyName { get; set; } = string.Empty;
        public string? CompanyAddress { get; set; }
        public string? Phone { get; set; }
        public string? Email { get; set; }
        public string? GSTNumber { get; set; }
        public string? Currency { get; set; }
        public string? InvoicePrefix { get; set; }
        public string? LogoUrl { get; set; }
    }
}