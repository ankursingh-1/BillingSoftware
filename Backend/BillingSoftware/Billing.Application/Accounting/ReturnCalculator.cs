namespace Billing.Application.Accounting;

/// <summary>The original line of the invoice (or purchase) being returned against.</summary>
public readonly record struct OriginalLine(
    int Quantity,
    decimal UnitPrice,
    decimal TaxRate,
    decimal Taxable,
    TaxBreakup Tax);

/// <summary>What earlier returns have already taken from the same line.</summary>
public readonly record struct ReturnedSoFar(
    int Quantity,
    decimal Taxable,
    TaxBreakup Tax)
{
    public static ReturnedSoFar None => new(0, 0m, new TaxBreakup(0m, 0m, 0m));
}

public readonly record struct ReturnLineAmounts(decimal Taxable, TaxBreakup Tax)
{
    public decimal Total => Taxable + Tax.Total;
}

/// <summary>
/// Credit and debit note amounts. A partial return is proportional to the original
/// line; the return that brings a line to its full quantity takes exactly what is left
/// of the original amounts. Rounding can therefore never leave a stray paisa, and the
/// returns of a line always add up to the original line.
/// </summary>
public static class ReturnCalculator
{
    public static ReturnLineAmounts Line(
        OriginalLine original,
        ReturnedSoFar returned,
        int quantity,
        bool isInterState)
    {
        if (quantity < 1)
            throw new ArgumentOutOfRangeException(nameof(quantity), "Return quantity must be at least 1.");

        var remaining = original.Quantity - returned.Quantity;

        if (quantity > remaining)
            throw new ArgumentOutOfRangeException(nameof(quantity), "Return quantity is more than what is left to return.");

        if (quantity == remaining)
        {
            // Never below zero: proportional rounding on earlier returns can
            // overshoot the original by a paisa.
            return new ReturnLineAmounts(
                Math.Max(0m, original.Taxable - returned.Taxable),
                new TaxBreakup(
                    Math.Max(0m, original.Tax.Cgst - returned.Tax.Cgst),
                    Math.Max(0m, original.Tax.Sgst - returned.Tax.Sgst),
                    Math.Max(0m, original.Tax.Igst - returned.Tax.Igst)));
        }

        var taxable = GstCalculator.LineTaxable(quantity, original.UnitPrice);
        var tax = GstCalculator.CalculateTax(taxable, original.TaxRate, isInterState);

        return new ReturnLineAmounts(taxable, tax);
    }

    /// <summary>
    /// Credit note value: the lines, plus the invoice round-off when this return
    /// finishes returning the whole document (so a full return equals the invoice total).
    /// </summary>
    public static decimal DocumentTotal(IEnumerable<ReturnLineAmounts> lines, bool completesDocument, decimal documentRoundOff)
    {
        var sum = lines.Sum(l => l.Total);
        return GstCalculator.Round(sum + (completesDocument ? documentRoundOff : 0m));
    }
}