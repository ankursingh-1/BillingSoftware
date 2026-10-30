using System.Reflection;
using Billing.API.Controllers;
using Billing.Application.Warranty;
using Billing.Domain.Enums;
using Billing.Shared.Security;
using Microsoft.AspNetCore.Authorization;

namespace Billing.Tests;

public class WarrantyTests
{
    [Fact]
    public void One_year_from_28_oct_ends_on_28_oct_next_year()
    {
        var start = new DateTime(2026, 10, 28);
        var end = WarrantyDates.EndDate(start, 1, WarrantyDurationUnit.Years);
        Assert.Equal(new DateTime(2027, 10, 28), end);
    }

    [Fact]
    public void Month_end_follows_clr_add_months()
    {
        var start = new DateTime(2026, 1, 31);
        var end = WarrantyDates.EndDate(start, 1, WarrantyDurationUnit.Months);
        Assert.Equal(new DateTime(2026, 2, 28), end);
    }

    [Fact]
    public void Days_remaining_and_status_use_stored_end_date()
    {
        var end = new DateTime(2026, 11, 10);
        var today = new DateTime(2026, 10, 28);
        Assert.Equal(13, WarrantyDates.DaysRemaining(end, today));
        Assert.Equal("ExpiringSoon", WarrantyDates.Status(end, today));
        Assert.Equal("Expired", WarrantyDates.Status(end, new DateTime(2026, 11, 11)));
        Assert.Equal("Active", WarrantyDates.Status(end.AddDays(40), today));
    }

    [Fact]
    public void Eligibility_is_inclusive_of_start_and_end_and_ignores_voided()
    {
        var start = new DateTime(2026, 1, 1);
        var end = new DateTime(2026, 12, 31);
        Assert.True(WarrantyDates.IsEligible(start, end, new DateTime(2026, 1, 1), false));
        Assert.True(WarrantyDates.IsEligible(start, end, new DateTime(2026, 12, 31), false));
        Assert.False(WarrantyDates.IsEligible(start, end, new DateTime(2027, 1, 1), false));
        Assert.False(WarrantyDates.IsEligible(start, end, new DateTime(2026, 6, 1), true));
    }

    [Theory]
    [InlineData(WarrantyClaimStatus.Submitted, WarrantyClaimStatus.UnderInspection, true)]
    [InlineData(WarrantyClaimStatus.Submitted, WarrantyClaimStatus.Approved, false)]
    [InlineData(WarrantyClaimStatus.UnderInspection, WarrantyClaimStatus.Approved, true)]
    [InlineData(WarrantyClaimStatus.Approved, WarrantyClaimStatus.ReplacementIssued, true)]
    [InlineData(WarrantyClaimStatus.Rejected, WarrantyClaimStatus.Approved, false)]
    [InlineData(WarrantyClaimStatus.ReplacementIssued, WarrantyClaimStatus.Closed, true)]
    [InlineData(WarrantyClaimStatus.Closed, WarrantyClaimStatus.Submitted, false)]
    public void Claim_transitions_are_restricted(
        WarrantyClaimStatus from, WarrantyClaimStatus to, bool allowed)
    {
        Assert.Equal(allowed, WarrantyClaimTransitions.CanMove(from, to));
    }

    [Fact]
    public void Warranty_api_requires_a_signed_in_user()
    {
        Assert.NotNull(typeof(WarrantyController).GetCustomAttribute<AuthorizeAttribute>());
        Assert.Null(typeof(WarrantyController).GetCustomAttribute<AuthorizeAttribute>()!.Roles);
    }

    [Fact]
    public void Day_window_is_half_open_and_includes_both_calendar_ends()
    {
        var start = new DateTime(2026, 10, 28);
        var endInclusive = new DateTime(2026, 11, 27);
        var (from, toExclusive) = WarrantyDates.DayWindow(start, endInclusive);

        Assert.Equal(start.Date, from);
        Assert.Equal(new DateTime(2026, 11, 28), toExclusive);

        var firstDay = start.Date;
        var lastDay = endInclusive.Date;
        var before = firstDay.AddDays(-1);
        var after = lastDay.AddDays(1);
        Assert.True(firstDay >= from && firstDay < toExclusive);
        Assert.True(lastDay >= from && lastDay < toExclusive);
        Assert.False(before >= from && before < toExclusive);
        Assert.False(after >= from && after < toExclusive);
    }

    [Fact]
    public void Product_scan_route_exists()
    {
        Assert.Contains("scan", AuthorizationAttributeTestsRoutes());
    }

    private static List<string> AuthorizationAttributeTestsRoutes()
    {
        return typeof(ProductsController).GetMethods()
            .SelectMany(m => m.GetCustomAttributes(typeof(Microsoft.AspNetCore.Mvc.HttpGetAttribute), false)
                .Cast<Microsoft.AspNetCore.Mvc.HttpGetAttribute>()
                .Select(a => a.Template ?? ""))
            .ToList();
    }
}
