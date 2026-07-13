using Billing.Domain.Common;

namespace Billing.Domain.Entities;

public class Supplier : SoftDeleteEntity
{
    public string Name { get; set; } = string.Empty;
    public string CompanyName { get; set; } = string.Empty;
    public string Mobile { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? Address { get; set; }
}