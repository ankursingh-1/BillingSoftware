namespace Billing.Application.Accounting;

public static class DateTimeExtensions
{
    /// <summary>
    /// Marks a timestamp read from the database as UTC so it is serialised with a "Z"
    /// and clients show it in their own time zone. Only use it on columns that really
    /// hold UTC (CreatedOn, payment times, ledger times). Business dates such as
    /// SaleDate and PurchaseDate are local calendar dates and are left alone.
    /// </summary>
    public static DateTime AsUtc(this DateTime value) =>
        DateTime.SpecifyKind(value, DateTimeKind.Utc);

    public static DateTime? AsUtc(this DateTime? value) =>
        value.HasValue ? DateTime.SpecifyKind(value.Value, DateTimeKind.Utc) : null;
}