using Billing.Application.Accounting;

namespace Billing.Tests;

public class GstCalculatorTests
{
    [Theory]
    [InlineData(1, 99.99, 99.99)]
    [InlineData(3, 33.33, 99.99)]
    [InlineData(3, 0.335, 1.01)]   // half rounds away from zero
    [InlineData(7, 10.005, 70.04)]
    public void LineTaxable_rounds_to_two_decimals(int qty, double price, double expected) =>
        Assert.Equal((decimal)expected, GstCalculator.LineTaxable(qty, (decimal)price));

    [Fact]
    public void Intra_state_splits_rate_equally_into_cgst_and_sgst()
    {
        var tax = GstCalculator.CalculateTax(1000m, 18m, isInterState: false);

        Assert.Equal(90m, tax.Cgst);
        Assert.Equal(90m, tax.Sgst);
        Assert.Equal(0m, tax.Igst);
        Assert.Equal(180m, tax.Total);
    }

    [Fact]
    public void Inter_state_charges_igst_only()
    {
        var tax = GstCalculator.CalculateTax(1000m, 18m, isInterState: true);

        Assert.Equal(0m, tax.Cgst);
        Assert.Equal(0m, tax.Sgst);
        Assert.Equal(180m, tax.Igst);
    }

    [Fact]
    public void Intra_state_halves_are_rounded_individually_and_stay_equal()
    {
        // 18.5 x 5% = 0.925 -> each half 0.4625 -> 0.46; tax = 0.92 (not 0.93)
        var tax = GstCalculator.CalculateTax(18.50m, 5m, isInterState: false);

        Assert.Equal(0.46m, tax.Cgst);
        Assert.Equal(tax.Cgst, tax.Sgst);
        Assert.Equal(0.92m, tax.Total);
    }

    [Theory]
    [InlineData(0, 18)]
    [InlineData(100, 0)]
    public void Zero_taxable_or_zero_rate_gives_no_tax(double taxable, double rate)
    {
        var tax = GstCalculator.CalculateTax((decimal)taxable, (decimal)rate, false);
        Assert.Equal(0m, tax.Total);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(100.01)]
    public void Invalid_rate_is_rejected(double rate) =>
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            GstCalculator.CalculateTax(100m, (decimal)rate, false));

    [Fact]
    public void Document_total_is_rounded_to_the_nearest_rupee_with_round_off()
    {
        var lines = new[]
        {
            (Taxable: 100.00m, Tax: GstCalculator.CalculateTax(100.00m, 18m, false)),
            (Taxable: 49.99m,  Tax: GstCalculator.CalculateTax(49.99m, 5m, false)),
        };

        var totals = GstCalculator.Total(lines);

        // 100 + 18.00 + 49.99 + (49.99 x 5% = 2.4995 -> 1.25 + 1.25 = 2.50) = 170.49 -> 170
        Assert.Equal(149.99m, totals.Subtotal);
        Assert.Equal(20.50m, totals.Tax);
        Assert.Equal(170m, totals.GrandTotal);
        Assert.Equal(-0.49m, totals.RoundOff);
        Assert.Equal(totals.Subtotal + totals.Tax + totals.RoundOff, totals.GrandTotal);
    }

    [Fact]
    public void Half_rupee_rounds_up()
    {
        var totals = GstCalculator.Total(new[] { (Taxable: 100.50m, Tax: default(TaxBreakup)) });

        Assert.Equal(101m, totals.GrandTotal);
        Assert.Equal(0.50m, totals.RoundOff);
    }

    [Fact]
    public void Grand_total_always_equals_subtotal_plus_tax_plus_round_off()
    {
        var random = new Random(42);
        for (var i = 0; i < 500; i++)
        {
            var lines = Enumerable.Range(0, random.Next(1, 8)).Select(_ =>
            {
                var taxable = GstCalculator.LineTaxable(random.Next(1, 20), random.Next(1, 100000) / 100m);
                var rate = new[] { 0m, 5m, 12m, 18m, 28m }[random.Next(5)];
                return (taxable, GstCalculator.CalculateTax(taxable, rate, random.Next(2) == 0));
            }).ToList();

            var t = GstCalculator.Total(lines);

            Assert.Equal(t.GrandTotal, t.Subtotal + t.Tax + t.RoundOff);
            Assert.Equal(t.Tax, t.Cgst + t.Sgst + t.Igst);
            Assert.True(Math.Abs(t.RoundOff) <= 0.5m);
            Assert.Equal(t.GrandTotal, Math.Round(t.GrandTotal, 0));
        }
    }
}
