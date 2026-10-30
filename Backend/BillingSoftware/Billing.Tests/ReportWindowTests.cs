using Billing.Application.Accounting;

namespace Billing.Tests;

public class ReportWindowTests
{
    [Fact]
    public void End_date_includes_the_whole_calendar_day()
    {
        var endOfDay = new DateTime(2026, 10, 30, 18, 45, 0);
        var midnight = new DateTime(2026, 10, 30);

        Assert.Equal(new DateTime(2026, 10, 31), ReportWindow.EndExclusive(endOfDay));
        Assert.Equal(new DateTime(2026, 10, 31), ReportWindow.EndExclusive(midnight));
        Assert.Equal(midnight, ReportWindow.StartOfDay(endOfDay));
        Assert.True(endOfDay < ReportWindow.EndExclusive(midnight));
        Assert.False(endOfDay <= midnight);

        var endOfCalendarDay = new DateTime(2026, 10, 30, 23, 59, 59);
        var nextMidnight = new DateTime(2026, 10, 31);
        Assert.True(endOfCalendarDay < ReportWindow.EndExclusive(midnight));
        Assert.False(nextMidnight < ReportWindow.EndExclusive(midnight));
    }

    [Fact]
    public void Sales_and_purchase_reports_use_the_exclusive_end()
    {
        var src = File.ReadAllText(Find("ReportService.cs"));

        Assert.Contains("ReportWindow.EndExclusive", src);
        Assert.DoesNotContain("SaleDate <= request.ToDate", src);
        Assert.DoesNotContain("PurchaseDate <= request.ToDate", src);
    }

    [Fact]
    public void Exports_start_at_page_one()
    {
        var src = File.ReadAllText(Find("ReportsController.cs"));

        Assert.Contains("PrepareExport(request)", src);
        Assert.Contains("request.PageNumber = 1", src);
        Assert.Equal(1, src.Split("request.PageSize = 10_000").Length - 1);
        Assert.Contains("NormalizePage", File.ReadAllText(Find("ReportService.cs")));

        var expenses = File.ReadAllText(Find("ExpenseService.cs"));
        Assert.Contains("private const int MaxPageSize = 10_000", expenses);
        Assert.DoesNotContain("private const int MaxPageSize = 100;", expenses);
    }

    private static string Find(string fileName)
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir != null)
        {
            var matches = dir.GetFiles(fileName, SearchOption.AllDirectories);
            if (matches.Length > 0)
                return matches[0].FullName;
            dir = dir.Parent;
        }

        throw new FileNotFoundException(fileName);
    }
}
