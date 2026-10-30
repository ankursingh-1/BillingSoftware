namespace Billing.Application.Retail;

/// <summary>
/// Walk-in is a real customer row because Sale.CustomerId is required.
/// Grocery checkout selects this customer instead of leaving the sale without one.
/// </summary>
public static class WalkInCustomer
{
    public const string Name = "Walk-in Customer";
    public const string Mobile = "9000000000";

    public static bool IsWalkIn(string? name, string? mobile) =>
        string.Equals(mobile?.Trim(), Mobile, StringComparison.Ordinal)
        || string.Equals(name?.Trim(), Name, StringComparison.OrdinalIgnoreCase);
}
