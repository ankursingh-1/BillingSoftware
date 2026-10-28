using Billing.Application.Accounting;

namespace Billing.Tests;

public class FinancialYearAndNumberingTests
{
    [Theory]
    [InlineData("2026-04-01", "2627")]
    [InlineData("2026-10-28", "2627")]
    [InlineData("2027-03-31", "2627")]
    [InlineData("2027-04-01", "2728")]
    [InlineData("2026-03-31", "2526")]
    [InlineData("2026-01-01", "2526")]
    [InlineData("2099-12-31", "9900")]
    public void Financial_year_code_runs_april_to_march(string date, string code) =>
        Assert.Equal(code, FinancialYear.Code(DateTime.Parse(date)));

    [Fact]
    public void Label_is_human_readable() =>
        Assert.Equal("2026-27", FinancialYear.Label(new DateTime(2026, 10, 28)));

    [Fact]
    public void Invoice_number_matches_the_agreed_format() =>
        Assert.Equal("INV-2627-000001",
            DocumentNumbering.Format("INV", new DateTime(2026, 10, 28), 1));

    [Fact]
    public void Four_character_prefix_still_fits_the_16_character_limit()
    {
        var number = DocumentNumbering.Format("ABCD", new DateTime(2026, 10, 28), 999_999);
        Assert.Equal(16, number.Length);
    }

    [Fact]
    public void Series_exhaustion_is_reported_not_silently_overflowed() =>
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            DocumentNumbering.Format("INV", new DateTime(2026, 10, 28), 1_000_000));

    [Fact]
    public void Too_long_prefix_is_rejected_by_format() =>
        Assert.Throws<ArgumentException>(() =>
            DocumentNumbering.Format("ABCDE", new DateTime(2026, 10, 28), 1));

    [Theory]
    [InlineData("inv", "INV")]
    [InlineData(" AB1 ", "AB1")]
    [InlineData("A", "A")]
    public void Prefix_is_normalised(string input, string expected) =>
        Assert.Equal(expected, DocumentNumbering.NormalizePrefix(input));

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    [InlineData("ABCDE")]
    [InlineData("IN-V")]
    [InlineData("IN V")]
    [InlineData("आईएन")]
    public void Invalid_prefix_is_rejected(string? input) =>
        Assert.Null(DocumentNumbering.NormalizePrefix(input));

    [Fact]
    public void Series_key_changes_with_financial_year_not_calendar_year()
    {
        Assert.Equal(DocumentNumbering.InvoiceSeriesKey(new DateTime(2026, 10, 1)),
                     DocumentNumbering.InvoiceSeriesKey(new DateTime(2027, 2, 1)));
        Assert.NotEqual(DocumentNumbering.InvoiceSeriesKey(new DateTime(2027, 3, 31)),
                        DocumentNumbering.InvoiceSeriesKey(new DateTime(2027, 4, 1)));
    }
}
