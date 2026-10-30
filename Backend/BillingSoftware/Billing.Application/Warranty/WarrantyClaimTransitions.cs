using Billing.Domain.Enums;

namespace Billing.Application.Warranty;

public static class WarrantyClaimTransitions
{
    private static readonly Dictionary<WarrantyClaimStatus, WarrantyClaimStatus[]> Allowed = new()
    {
        [WarrantyClaimStatus.Submitted] =
        [
            WarrantyClaimStatus.UnderInspection,
            WarrantyClaimStatus.Rejected,
            WarrantyClaimStatus.Closed
        ],
        [WarrantyClaimStatus.UnderInspection] =
        [
            WarrantyClaimStatus.Approved,
            WarrantyClaimStatus.Rejected,
            WarrantyClaimStatus.Closed
        ],
        [WarrantyClaimStatus.Approved] =
        [
            WarrantyClaimStatus.Repaired,
            WarrantyClaimStatus.ReplacementIssued,
            WarrantyClaimStatus.Closed
        ],
        [WarrantyClaimStatus.Rejected] = [WarrantyClaimStatus.Closed],
        [WarrantyClaimStatus.Repaired] = [WarrantyClaimStatus.Closed],
        [WarrantyClaimStatus.ReplacementIssued] = [WarrantyClaimStatus.Closed],
        [WarrantyClaimStatus.Closed] = []
    };

    public static bool CanMove(WarrantyClaimStatus from, WarrantyClaimStatus to) =>
        from == to || (Allowed.TryGetValue(from, out var next) && next.Contains(to));

    public static bool IsOpen(WarrantyClaimStatus status) =>
        status is not WarrantyClaimStatus.Closed and not WarrantyClaimStatus.Rejected;

    public static bool IsTerminal(WarrantyClaimStatus status) =>
        status is WarrantyClaimStatus.Closed or WarrantyClaimStatus.Rejected
            or WarrantyClaimStatus.Repaired or WarrantyClaimStatus.ReplacementIssued;
}
