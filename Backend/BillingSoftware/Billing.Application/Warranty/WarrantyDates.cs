using Billing.Domain.Enums;

namespace Billing.Application.Warranty;

/// <summary>
/// Warranty end dates are stored on the registration. Changing a product's
/// default later never recalculates historical rows.
///
/// EndDate = StartDate plus the duration (DateTime.AddDays / AddMonths / AddYears).
/// Coverage is inclusive of StartDate and EndDate. Month-end follows CLR:
/// 31 January + 1 month = 28 or 29 February.
/// </summary>
public static class WarrantyDates
{
    public const int ExpiringSoonDays = 30;

    public static DateTime EndDate(DateTime start, int value, WarrantyDurationUnit unit)
    {
        if (value <= 0)
            throw new ArgumentOutOfRangeException(nameof(value), "Warranty duration must be at least 1.");

        var day = start.Date;

        return unit switch
        {
            WarrantyDurationUnit.Days => day.AddDays(value),
            WarrantyDurationUnit.Months => day.AddMonths(value),
            WarrantyDurationUnit.Years => day.AddYears(value),
            _ => throw new ArgumentOutOfRangeException(nameof(unit), "Unknown warranty duration unit.")
        };
    }

    public static string Status(DateTime end, DateTime today)
    {
        var remaining = DaysRemaining(end, today);
        if (remaining < 0)
            return "Expired";
        if (remaining <= ExpiringSoonDays)
            return "ExpiringSoon";
        return "Active";
    }

    public static int DaysRemaining(DateTime end, DateTime today) =>
        (end.Date - today.Date).Days;

    public static bool IsEligible(DateTime start, DateTime end, DateTime today, bool voided)
    {
        if (voided)
            return false;

        var t = today.Date;
        return t >= start.Date && t <= end.Date;
    }

    public static string FormatDuration(int value, WarrantyDurationUnit unit)
    {
        var label = unit switch
        {
            WarrantyDurationUnit.Days => value == 1 ? "day" : "days",
            WarrantyDurationUnit.Months => value == 1 ? "month" : "months",
            WarrantyDurationUnit.Years => value == 1 ? "year" : "years",
            _ => unit.ToString().ToLowerInvariant()
        };

        return $"{value} {label}";
    }

    /// <summary>
    /// Inclusive calendar days as a half-open range <c>[from, toExclusive)</c>.
    /// EF Core translates this to SQL; <c>DateTime.Date</c> on a column often does not.
    /// </summary>
    public static (DateTime FromInclusive, DateTime ToExclusive) DayWindow(
        DateTime fromDay,
        DateTime toDayInclusive)
    {
        var from = fromDay.Date;
        var toExclusive = toDayInclusive.Date.AddDays(1);

        if (toExclusive <= from)
            throw new ArgumentException("The date window is empty.", nameof(toDayInclusive));

        return (from, toExclusive);
    }
}
