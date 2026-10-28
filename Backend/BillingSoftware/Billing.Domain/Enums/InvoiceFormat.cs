namespace Billing.Domain.Enums;

/// <summary>How an invoice is laid out when printed or exported.</summary>
public enum InvoiceFormat
{
    /// <summary>Full A4 tax invoice.</summary>
    A4 = 1,

    /// <summary>Short counter-billing invoice (A5-width page, independent layout).</summary>
    Compact = 2,

    /// <summary>Narrow roll receipt (58 mm or 80 mm, set separately).</summary>
    Thermal = 3
}