using Billing.Domain.Common;
using Billing.Domain.Enums;

namespace Billing.Domain.Entities;

public class WarrantyClaimEvent : AuditableEntity
{
    public int WarrantyClaimId { get; set; }
    public WarrantyClaimStatus Status { get; set; }
    public string? Notes { get; set; }

    public WarrantyClaim WarrantyClaim { get; set; } = null!;
}
