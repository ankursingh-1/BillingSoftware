namespace Billing.Domain.Entities;

/// <summary>
/// Gapless counter per document series, e.g. SeriesKey "INV:2627" for the
/// 2026-27 financial-year invoices. Incremented inside the same database
/// transaction that creates the document, so a rolled-back document gives
/// its number back.
/// </summary>
public class DocumentSequence
{
    public string SeriesKey { get; set; } = string.Empty;
    public int LastNumber { get; set; }
}
