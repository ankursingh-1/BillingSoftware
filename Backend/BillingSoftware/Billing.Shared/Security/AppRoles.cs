namespace Billing.Shared.Security;

/// <summary>Role names stored in the Roles table and carried in the JWT.</summary>
public static class AppRoles
{
    /// <summary>Full access: settings, users, purchases, expenses, reports, stock adjustments, voids.</summary>
    public const string Admin = "Admin";

    /// <summary>Billing operator: sales, payments, customers, sales returns, viewing stock.</summary>
    public const string Staff = "Staff";
}
