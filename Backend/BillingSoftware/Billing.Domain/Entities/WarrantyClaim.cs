using Billing.Domain.Common;
using Billing.Domain.Enums;

namespace Billing.Domain.Entities;

public class WarrantyClaim : AuditableEntity
{
    public string ClaimNumber { get; set; } = string.Empty;
    public int WarrantyId { get; set; }

    public WarrantyClaimStatus Status { get; set; } = WarrantyClaimStatus.Submitted;

    public string ReportedProblem { get; set; } = string.Empty;
    public string? InspectionNotes { get; set; }
    public string? DecisionReason { get; set; }
    public string? Notes { get; set; }

    public string? DefectiveSerial { get; set; }

    public int? ReplacementProductId { get; set; }
    public string? ReplacementProductName { get; set; }
    public string? ReplacementSerial { get; set; }

    /// <summary>True after the replacement stock movement has been written. Prevents a second deduction.</summary>
    public bool ReplacementStockMoved { get; set; }

    public DateTime SubmittedOn { get; set; }
    public DateTime? ResolvedOn { get; set; }

    public Warranty Warranty { get; set; } = null!;
    public Product? ReplacementProduct { get; set; }

    public ICollection<WarrantyClaimEvent> Events { get; set; } = new List<WarrantyClaimEvent>();
}
