using Billing.Application.Accounting;

namespace Billing.Tests;

public class GrossProfitCalculatorTests
{
    [Fact]
    public void Fully_costed_sale_is_tax_exclusive_revenue_minus_snapshot_cost()
    {
        var invoice = GrossProfitCalculator.ForInvoice(
        [
            new ProfitLineFact(2, 0, 200m, 80m)
        ]);

        Assert.True(invoice.CostComplete);
        Assert.Equal(200m, invoice.NetRevenue);
        Assert.Equal(160m, invoice.CostOfGoodsSold);
        Assert.Equal(40m, invoice.Profit);
        Assert.Equal(GrossProfitCalculator.CompleteStatus, invoice.CostStatus);
    }

    [Fact]
    public void Missing_cost_is_not_treated_as_zero()
    {
        var invoice = GrossProfitCalculator.ForInvoice(
        [
            new ProfitLineFact(1, 0, 100m, null)
        ]);

        Assert.False(invoice.CostComplete);
        Assert.Equal(100m, invoice.NetRevenue);
        Assert.Null(invoice.CostOfGoodsSold);
        Assert.Null(invoice.Profit);
        Assert.Equal(GrossProfitCalculator.UnavailableStatus, invoice.CostStatus);
    }

    [Fact]
    public void Mixed_lines_are_partial_and_invoice_profit_is_withheld()
    {
        var invoice = GrossProfitCalculator.ForInvoice(
        [
            new ProfitLineFact(1, 0, 100m, 40m),
            new ProfitLineFact(1, 0, 50m, null)
        ]);

        Assert.False(invoice.CostComplete);
        Assert.True(invoice.HasUnknownCost);
        Assert.Equal(150m, invoice.NetRevenue);
        Assert.Null(invoice.Profit);
        Assert.Equal(GrossProfitCalculator.PartialStatus, invoice.CostStatus);
    }

    [Fact]
    public void Catalogue_price_change_does_not_alter_the_snapshot()
    {
        var snap = CostSnapshot.FromCatalogue(80m);
        var laterCatalogue = 120m;

        var invoice = GrossProfitCalculator.ForInvoice(
        [
            new ProfitLineFact(1, 0, 200m, snap)
        ]);

        Assert.Equal(80m, invoice.CostOfGoodsSold);
        Assert.NotEqual(laterCatalogue, invoice.CostOfGoodsSold);
        Assert.Equal(120m, invoice.Profit);
    }

    [Fact]
    public void Partial_payment_is_not_an_input_so_profit_is_unchanged()
    {
        var unpaid = GrossProfitCalculator.ForInvoice(
        [
            new ProfitLineFact(1, 0, 100m, 40m)
        ]);
        var paid = GrossProfitCalculator.ForInvoice(
        [
            new ProfitLineFact(1, 0, 100m, 40m)
        ]);

        Assert.Equal(unpaid.Profit, paid.Profit);
        Assert.Equal(60m, paid.Profit);
    }

    [Fact]
    public void Return_reduces_revenue_and_cogs_and_does_not_count_returned_stock()
    {
        var full = GrossProfitCalculator.ForInvoice(
        [
            new ProfitLineFact(4, 0, 400m, 50m)
        ]);
        var after = GrossProfitCalculator.ForInvoice(
        [
            new ProfitLineFact(4, 1, 400m, 50m)
        ]);

        Assert.Equal(400m, full.NetRevenue);
        Assert.Equal(200m, full.CostOfGoodsSold);
        Assert.Equal(300m, after.NetRevenue);
        Assert.Equal(150m, after.CostOfGoodsSold);
        Assert.Equal(150m, after.Profit);
    }

    [Fact]
    public void Fully_returned_line_needs_no_cost()
    {
        var invoice = GrossProfitCalculator.ForInvoice(
        [
            new ProfitLineFact(2, 2, 100m, null)
        ]);

        Assert.True(invoice.CostComplete);
        Assert.Equal(0m, invoice.NetRevenue);
        Assert.Equal(0m, invoice.CostOfGoodsSold);
        Assert.Equal(0m, invoice.Profit);
    }

    [Fact]
    public void Summary_known_profit_uses_complete_invoices_only()
    {
        var complete = GrossProfitCalculator.ForInvoice(
        [
            new ProfitLineFact(1, 0, 100m, 40m)
        ]);
        var incomplete = GrossProfitCalculator.ForInvoice(
        [
            new ProfitLineFact(1, 0, 80m, null)
        ]);

        var summary = GrossProfitCalculator.Summarise([complete, incomplete], linesWithCost: 1, linesMissingCost: 1);

        Assert.Equal(180m, summary.NetRevenue);
        Assert.Equal(60m, summary.KnownGrossProfit);
        Assert.False(summary.CostComplete);
        Assert.Equal(1, summary.CompleteInvoices);
        Assert.Equal(1, summary.IncompleteInvoices);
        Assert.Equal(50m, summary.CoveragePercent);
        Assert.Contains("excluded", summary.Limitation, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("not calculated as sales minus purchases", summary.Limitation, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void All_incomplete_yields_null_known_profit_not_zero()
    {
        var incomplete = GrossProfitCalculator.ForInvoice(
        [
            new ProfitLineFact(1, 0, 80m, null)
        ]);
        var summary = GrossProfitCalculator.Summarise([incomplete], 0, 1);

        Assert.Null(summary.KnownGrossProfit);
        Assert.False(summary.CostComplete);
    }

    [Fact]
    public void Empty_period_is_complete_with_zero_profit()
    {
        var summary = GrossProfitCalculator.Summarise(Array.Empty<InvoiceProfit>());

        Assert.Equal(0m, summary.NetRevenue);
        Assert.Equal(0m, summary.KnownGrossProfit);
        Assert.True(summary.CostComplete);
        Assert.Equal(100m, summary.CoveragePercent);
        Assert.Null(summary.Limitation);
    }

    [Fact]
    public void Rounding_stays_at_two_decimals()
    {
        var invoice = GrossProfitCalculator.ForInvoice(
        [
            new ProfitLineFact(3, 1, 100.00m, 10.005m)
        ]);

        Assert.Equal(66.67m, invoice.NetRevenue);
        Assert.Equal(20.01m, invoice.CostOfGoodsSold);
    }

    [Fact]
    public void Zero_catalogue_price_is_unknown_not_free()
    {
        Assert.Null(CostSnapshot.FromCatalogue(0m));
        Assert.Equal(12.50m, CostSnapshot.FromCatalogue(12.5m));
    }

    [Fact]
    public void Backfill_uses_last_purchase_on_or_before_sale_and_never_overwrites()
    {
        var sale = new DateTime(2026, 10, 28);
        var purchases = new (DateTime, decimal)[]
        {
            (new DateTime(2026, 10, 1), 80m),
            (new DateTime(2026, 11, 1), 99m)
        };

        Assert.Equal(80m, CostBackfill.Propose(null, sale, purchases));
        Assert.Equal(40m, CostBackfill.Propose(40m, sale, purchases));
        Assert.Null(CostBackfill.Propose(null, sale, Array.Empty<(DateTime, decimal)>()));
    }

    [Fact]
    public void Zero_stored_cost_is_unknown_not_free()
    {
        var invoice = GrossProfitCalculator.ForInvoice(
        [
            new ProfitLineFact(1, 0, 100m, 0m)
        ]);

        Assert.False(invoice.CostComplete);
        Assert.Null(invoice.Profit);
        Assert.Equal(GrossProfitCalculator.UnavailableStatus, invoice.CostStatus);
    }

    [Fact]
    public void Reconstructed_purchase_cost_completes_the_invoice()
    {
        var cost = CostBackfill.ForProfit(
            null,
            new DateTime(2026, 10, 28),
            [(new DateTime(2026, 10, 1), 40m)]);

        var invoice = GrossProfitCalculator.ForInvoice(
        [
            new ProfitLineFact(1, 0, 100m, cost)
        ]);

        Assert.Equal(40m, cost);
        Assert.True(invoice.CostComplete);
        Assert.Equal(60m, invoice.Profit);
    }

    [Fact]
    public void Opening_stock_without_purchase_history_stays_incomplete()
    {
        var cost = CostBackfill.ForProfit(
            null,
            new DateTime(2026, 10, 28),
            Array.Empty<(DateTime, decimal)>());

        var invoice = GrossProfitCalculator.ForInvoice(
        [
            new ProfitLineFact(1, 0, 100m, cost)
        ]);

        Assert.Null(cost);
        Assert.False(invoice.CostComplete);
        Assert.Null(invoice.Profit);
    }

    [Fact]
    public void Profit_cost_prefers_sale_line_snapshot_over_a_later_purchase()
    {
        var sale = new DateTime(2026, 10, 28);
        var purchases = new (DateTime, decimal)[]
        {
            (new DateTime(2026, 10, 1), 80m)
        };

        Assert.Equal(40m, CostBackfill.ForProfit(40m, sale, purchases));
        Assert.Equal(80m, CostBackfill.ForProfit(null, sale, purchases));
        Assert.Equal(80m, CostBackfill.ForProfit(0m, sale, purchases));
        Assert.Null(CostBackfill.ForProfit(null, sale, Array.Empty<(DateTime, decimal)>()));
    }

    [Fact]
    public void Profit_cost_does_not_use_todays_catalogue_price()
    {
        var reconstructed = CostBackfill.ForProfit(
            null,
            new DateTime(2026, 10, 28),
            [(new DateTime(2026, 10, 1), 80m)]);

        const decimal todaysCatalogue = 999m;
        Assert.Equal(80m, reconstructed);
        Assert.NotEqual(todaysCatalogue, reconstructed);
    }

    [Fact]
    public void Backfill_does_not_use_a_purchase_after_the_sale()
    {
        var proposed = CostBackfill.Propose(
            null,
            new DateTime(2026, 7, 1),
            [(new DateTime(2026, 8, 1), 50m)]);

        Assert.Null(proposed);
    }

    [Fact]
    public void Date_range_totals_include_only_invoices_passed_in()
    {
        var october = GrossProfitCalculator.ForInvoice(
        [
            new ProfitLineFact(1, 0, 100m, 40m)
        ]);
        var november = GrossProfitCalculator.ForInvoice(
        [
            new ProfitLineFact(1, 0, 200m, 50m)
        ]);

        var octoberOnly = GrossProfitCalculator.Summarise([october]);
        var both = GrossProfitCalculator.Summarise([october, november]);

        Assert.Equal(60m, octoberOnly.KnownGrossProfit);
        Assert.Equal(210m, both.KnownGrossProfit);
        Assert.NotEqual(both.NetRevenue, octoberOnly.NetRevenue);
    }

    [Fact]
    public void Known_profit_is_not_sales_minus_purchases()
    {
        var invoice = GrossProfitCalculator.ForInvoice(
        [
            new ProfitLineFact(1, 0, 100m, 40m)
        ]);
        const decimal unrelatedPurchaseTotal = 999m;

        Assert.Equal(60m, invoice.Profit);
        Assert.NotEqual(100m - unrelatedPurchaseTotal, invoice.Profit);
    }

    [Fact]
    public void Gst_collected_is_not_treated_as_revenue()
    {
        const decimal taxable = 100m;
        const decimal taxInclusive = 118m;
        var invoice = GrossProfitCalculator.ForInvoice(
        [
            new ProfitLineFact(1, 0, taxable, 40m)
        ]);

        Assert.Equal(taxable, invoice.NetRevenue);
        Assert.NotEqual(taxInclusive, invoice.NetRevenue);
        Assert.Equal(60m, invoice.Profit);
    }

    [Fact]
    public void Dashboard_and_reports_use_the_same_summary_values()
    {
        var invoices = new[]
        {
            GrossProfitCalculator.ForInvoice([new ProfitLineFact(1, 0, 100m, 40m)]),
            GrossProfitCalculator.ForInvoice([new ProfitLineFact(1, 0, 80m, null)])
        };

        var dashboard = GrossProfitCalculator.Summarise(invoices, 1, 1);
        var reports = GrossProfitCalculator.Summarise(invoices, 1, 1);

        Assert.Equal(dashboard.KnownGrossProfit, reports.KnownGrossProfit);
        Assert.Equal(dashboard.NetRevenue, reports.NetRevenue);
        Assert.Equal(dashboard.CostComplete, reports.CostComplete);
        Assert.Equal(dashboard.Limitation, reports.Limitation);
    }
}
