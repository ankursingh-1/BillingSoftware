using System.Text.RegularExpressions;

namespace Billing.Application.Accounting;

/// <summary>
/// Document number format: PREFIX-FYCODE-000001, e.g. INV-2627-000001.
/// GST rules limit an invoice number to 16 characters, so the prefix is
/// limited to 4 characters (4 + 1 + 4 + 1 + 6 = 16).
/// </summary>
public static class DocumentNumbering
{
    public const int MaxLength = 16;
    public const int MaxPrefixLength = 4;
    public const int MaxSequence = 999_999;

    public const string PurchasePrefix = "PUR";
    public const string ReceiptPrefix = "RCT";         // payment receipt
    public const string SalesReturnPrefix = "CN";      // credit note
    public const string PurchaseReturnPrefix = "DN";   // debit note

    private static readonly Regex PrefixFormat = new("^[A-Z0-9]{1,4}$", RegexOptions.Compiled);

    /// <summary>Normalises a prefix (trim, upper-case). Returns null when it is not valid.</summary>
    public static string? NormalizePrefix(string? prefix)
    {
        var value = prefix?.Trim().ToUpperInvariant();
        return value != null && PrefixFormat.IsMatch(value) ? value : null;
    }

    public static string SeriesKey(string prefixOrType, DateTime date) =>
        $"{prefixOrType}:{FinancialYear.Code(date)}";

    /// <summary>Series key for sales invoices (one counter per financial year, independent of the prefix text).</summary>
    public static string InvoiceSeriesKey(DateTime date) => SeriesKey("INV", date);

    public static string Format(string prefix, DateTime date, int sequence)
    {
        if (sequence < 1 || sequence > MaxSequence)
            throw new ArgumentOutOfRangeException(nameof(sequence), "Document number series is exhausted for this financial year.");

        var number = $"{prefix}-{FinancialYear.Code(date)}-{sequence:000000}";

        if (number.Length > MaxLength)
            throw new ArgumentException($"Document number '{number}' exceeds {MaxLength} characters.", nameof(prefix));

        return number;
    }
}
