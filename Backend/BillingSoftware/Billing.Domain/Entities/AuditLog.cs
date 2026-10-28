using Billing.Domain.Common;

namespace Billing.Domain.Entities;

/// <summary>Append-only record of an important action. Never updated or deleted by the API.</summary>
public class AuditLog : BaseEntity
{
    public DateTime OccurredOn { get; set; }
    public int? UserId { get; set; }
    public string? UserEmail { get; set; }
    public string Action { get; set; } = string.Empty;
    public string? EntityType { get; set; }
    public string? EntityId { get; set; }
    public string? Details { get; set; }
    public string? IpAddress { get; set; }
}
