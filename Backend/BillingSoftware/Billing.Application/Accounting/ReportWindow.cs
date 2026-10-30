namespace Billing.Application.Accounting;

/// <summary>
/// Calendar-day bounds for report filters. A date-only "to" value arrives at
/// midnight, so <c>SaleDate &lt;= to</c> drops every document later that day.
/// </summary>
public static class ReportWindow
{
    public static DateTime StartOfDay(DateTime value) => value.Date;

    public static DateTime EndExclusive(DateTime toDate) => toDate.Date.AddDays(1);
}
