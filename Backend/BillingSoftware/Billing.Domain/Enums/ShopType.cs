namespace Billing.Domain.Enums;

/// <summary>
/// How the shop bills at the counter. One company, one application — this only
/// changes defaults (walk-in customer, warranty prompts), not separate apps.
/// </summary>
public enum ShopType
{
    GeneralRetail = 1,
    Electronics = 2,
    Grocery = 3
}
