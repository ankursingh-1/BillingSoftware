using Billing.Application.Accounting;
using Xunit;

namespace Billing.Tests;

public class ReturnCalculatorTests
{
    // 3 x 99.99 at 5% intra-state: taxable 299.97, CGST 7.50 (7.49925), SGST 7.50
    private static OriginalLine Original(bool inter = false)
    {
        var taxable = GstCalculator.LineTaxable(3, 99.99m);
        return new OriginalLine(3, 99.99m, 5m, taxable, GstCalculator.CalculateTax(taxable, 5m, inter));
    }

    [Fact]
    public void Partial_return_is_proportional()
    {
        var line = ReturnCalculator.Line(Original(), ReturnedSoFar.None, 1, false);

        Assert.Equal(99.99m, line.Taxable);
        Assert.Equal(GstCalculator.CalculateTax(99.99m, 5m, false), line.Tax);
    }

    [Fact]
    public void Returns_of_a_line_add_up_to_the_original_exactly()
    {
        var original = Original();
        var returned = ReturnedSoFar.None;
        decimal taxable = 0, cgst = 0, sgst = 0;

        for (var i = 0; i < 3; i++)
        {
            var part = ReturnCalculator.Line(original, returned, 1, false);
            taxable += part.Taxable; cgst += part.Tax.Cgst; sgst += part.Tax.Sgst;
            returned = new ReturnedSoFar(returned.Quantity + 1, returned.Taxable + part.Taxable,
                new TaxBreakup(returned.Tax.Cgst + part.Tax.Cgst, returned.Tax.Sgst + part.Tax.Sgst, returned.Tax.Igst + part.Tax.Igst));
        }

        Assert.Equal(original.Taxable, taxable);
        Assert.Equal(original.Tax.Cgst, cgst);
        Assert.Equal(original.Tax.Sgst, sgst);
    }

    [Fact]
    public void Last_return_takes_the_remainder_not_a_proportion()
    {
        var original = Original();
        var first = ReturnCalculator.Line(original, ReturnedSoFar.None, 2, false);
        var returned = new ReturnedSoFar(2, first.Taxable, first.Tax);

        var last = ReturnCalculator.Line(original, returned, 1, false);

        Assert.Equal(original.Taxable - first.Taxable, last.Taxable);
        Assert.Equal(original.Tax.Cgst - first.Tax.Cgst, last.Tax.Cgst);
    }

    [Fact]
    public void Inter_state_return_uses_igst()
    {
        var line = ReturnCalculator.Line(Original(inter: true), ReturnedSoFar.None, 1, true);

        Assert.Equal(0m, line.Tax.Cgst);
        Assert.Equal(0m, line.Tax.Sgst);
        Assert.Equal(GstCalculator.CalculateTax(99.99m, 5m, true).Igst, line.Tax.Igst);
    }

    [Fact]
    public void Legacy_line_without_tax_returns_the_untaxed_amount()
    {
        var original = new OriginalLine(2, 50m, 0m, 100m, new TaxBreakup(0m, 0m, 0m));

        var half = ReturnCalculator.Line(original, ReturnedSoFar.None, 1, false);
        var rest = ReturnCalculator.Line(original, new ReturnedSoFar(1, half.Taxable, half.Tax), 1, false);

        Assert.Equal(50m, half.Total);
        Assert.Equal(50m, rest.Total);
    }

    [Fact]
    public void Rounding_overshoot_never_makes_the_remainder_negative()
    {
        var original = new OriginalLine(2, 1m, 18m, 2m, new TaxBreakup(0.18m, 0.18m, 0m));
        // Pretend earlier returns took slightly more tax than the original line.
        var returned = new ReturnedSoFar(1, 1m, new TaxBreakup(0.19m, 0.19m, 0m));

        var last = ReturnCalculator.Line(original, returned, 1, false);

        Assert.Equal(0m, last.Tax.Cgst);
        Assert.Equal(0m, last.Tax.Sgst);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(4)]
    public void Impossible_quantities_are_rejected(int quantity)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            ReturnCalculator.Line(Original(), ReturnedSoFar.None, quantity, false));
    }

    [Fact]
    public void Quantity_over_what_is_left_is_rejected()
    {
        var returned = new ReturnedSoFar(2, 199.98m, new TaxBreakup(5m, 5m, 0m));

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            ReturnCalculator.Line(Original(), returned, 2, false));
    }

    [Fact]
    public void Completing_the_document_adds_the_invoice_round_off()
    {
        var lines = new[] { new ReturnLineAmounts(299.97m, new TaxBreakup(7.5m, 7.5m, 0m)) };

        Assert.Equal(314.97m, ReturnCalculator.DocumentTotal(lines, false, 0.03m));
        Assert.Equal(315.00m, ReturnCalculator.DocumentTotal(lines, true, 0.03m));
    }

    [Fact]
    public void Negative_round_off_is_applied_when_completing()
    {
        var lines = new[] { new ReturnLineAmounts(100m, new TaxBreakup(9.2m, 9.2m, 0m)) };

        Assert.Equal(118m, ReturnCalculator.DocumentTotal(lines, true, -0.40m));
    }
}