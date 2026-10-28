namespace Billing.Application.Accounting;

/// <summary>Indian financial year: 1 April to 31 March.</summary>
public static class FinancialYear
{
    public static int StartYear(DateTime date) =>
        date.Month >= 4 ? date.Year : date.Year - 1;

    /// <summary>Compact code used in document numbers, e.g. "2627" for 2026-27.</summary>
    public static string Code(DateTime date)
    {
        var start = StartYear(date);
        return $"{start % 100:00}{(start + 1) % 100:00}";
    }

    /// <summary>Human label, e.g. "2026-27".</summary>
    public static string Label(DateTime date)
    {
        var start = StartYear(date);
        return $"{start}-{(start + 1) % 100:00}";
    }

    public static DateTime StartDate(int startYear) => new(startYear, 4, 1);

    public static DateTime EndDate(int startYear) => new(startYear + 1, 3, 31);
}
