using Billing.Domain.Common;

namespace Billing.Domain.Entities;

public class Customer : SoftDeleteEntity
{
    public string Name { get; set; } = string.Empty;
    public string Mobile { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? Address { get; set; }

    /// <summary>State name (used to decide CGST/SGST vs IGST).</summary>
    public string? State { get; set; }

    /// <summary>Two-digit GST state code, e.g. "36" for Telangana.</summary>
    public string? StateCode { get; set; }

    /// <summary>15-character GSTIN for business (B2B) customers.</summary>
    public string? Gstin { get; set; }
}
