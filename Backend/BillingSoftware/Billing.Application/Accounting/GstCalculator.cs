namespace Billing.Application.Accounting;

/// <summary>Tax split of one line. Intra-state fills Cgst+Sgst, inter-state fills Igst.</summary>
public readonly record struct TaxBreakup(decimal Cgst, decimal Sgst, decimal Igst)
{
    public decimal Total => Cgst + Sgst + Igst;
}

/// <summary>Totals of a whole invoice (sale, purchase or return).</summary>
public readonly record struct DocumentTotals(
    decimal Subtotal,
    decimal Cgst,
    decimal Sgst,
    decimal Igst,
    decimal Tax,
    decimal RoundOff,
    decimal GrandTotal);

/// <summary>
/// The single place where money and GST rules live. Prices are tax-exclusive:
/// tax is added on top of the taxable value.
///
/// Rules:
///  - every amount is rounded to 2 decimals, half away from zero;
///  - intra-state: CGST = SGST = taxable x rate / 200 (each rounded), tax = CGST + SGST;
///  - inter-state: IGST = taxable x rate / 100 (rounded);
///  - tax is calculated per line and rounded per line; document totals are sums of the lines;
///  - the grand total is rounded to the nearest rupee, the difference is the round-off.
/// </summary>
public static class GstCalculator
{
    public static decimal Round(decimal value) =>
        Math.Round(value, 2, MidpointRounding.AwayFromZero);

    /// <summary>Taxable value of a line: quantity x unit price, rounded to 2 decimals.</summary>
    public static decimal LineTaxable(int quantity, decimal unitPrice) =>
        Round(quantity * unitPrice);

    public static TaxBreakup CalculateTax(decimal taxable, decimal ratePercent, bool isInterState)
    {
        if (ratePercent < 0 || ratePercent > 100)
            throw new ArgumentOutOfRangeException(nameof(ratePercent), "Tax rate must be between 0 and 100.");

        if (taxable == 0 || ratePercent == 0)
            return default;

        if (isInterState)
            return new TaxBreakup(0, 0, Round(taxable * ratePercent / 100m));

        var half = Round(taxable * ratePercent / 200m);
        return new TaxBreakup(half, half, 0);
    }

    /// <summary>Adds up line figures and rounds the invoice to the nearest rupee.</summary>
    public static DocumentTotals Total(IEnumerable<(decimal Taxable, TaxBreakup Tax)> lines)
    {
        decimal subtotal = 0, cgst = 0, sgst = 0, igst = 0;

        foreach (var (taxable, tax) in lines)
        {
            subtotal += taxable;
            cgst += tax.Cgst;
            sgst += tax.Sgst;
            igst += tax.Igst;
        }

        var totalTax = cgst + sgst + igst;
        var exact = subtotal + totalTax;
        var grand = Math.Round(exact, 0, MidpointRounding.AwayFromZero);

        return new DocumentTotals(subtotal, cgst, sgst, igst, totalTax, grand - exact, grand);
    }
}
