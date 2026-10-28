using System.Globalization;
using Billing.Application.DTOs.Sales;
using QuestPDF.Fluent;
using QuestPDF.Infrastructure;

namespace Billing.API.Services;

/// <summary>Formatting helpers shared by the invoice and receipt PDF layouts.</summary>
internal static class PdfParts
{
    public const string Ink = "#1f2937";
    public const string Muted = "#6b7280";
    public const string Rule = "#cbd5e1";
    public const string Fill = "#f1f5f9";
    public const string Accent = "#0f3d5e";

    private static readonly CultureInfo India = CultureInfo.GetCultureInfo("en-IN");

    public static float Mm(float millimetres) => millimetres * 72f / 25.4f;

    public static string Money(decimal value) => value.ToString("N2", India);

    public static string Rs(decimal value) => $"Rs. {Money(value)}";

    public static string? Prefixed(string label, string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : $"{label}: {value.Trim()}";

    public static string Join(string separator, params string?[] parts) =>
        string.Join(separator, parts.Where(p => !string.IsNullOrWhiteSpace(p)).Select(p => p!.Trim()));

    public static IEnumerable<string> AddressLines(InvoiceCompanyDto c)
    {
        if (!string.IsNullOrWhiteSpace(c.Address))
            yield return c.Address.Trim();

        var place = Join(", ", c.City, c.State, c.Pincode);
        if (place.Length > 0)
            yield return place;
    }

    public static string? ContactLine(InvoiceCompanyDto c, string separator = "  |  ")
    {
        var line = Join(separator, Prefixed("Ph", c.Phone), Prefixed("Email", c.Email));
        return line.Length == 0 ? null : line;
    }

    public static string? TaxIdLine(InvoiceCompanyDto c, string separator = "  |  ")
    {
        var line = Join(separator, Prefixed("GSTIN", c.Gstin), Prefixed("PAN", c.Pan));
        return line.Length == 0 ? null : line;
    }

    public static IEnumerable<string> BankLines(InvoiceCompanyDto c)
    {
        var bank = Join("  ", Prefixed("Bank", c.BankName), Prefixed("A/c", c.AccountNumber), Prefixed("IFSC", c.IfscCode));
        if (bank.Length > 0)
            yield return bank;

        if (!string.IsNullOrWhiteSpace(c.UpiId))
            yield return $"UPI: {c.UpiId.Trim()}";
    }

    public static string MethodLabel(string method) => method switch
    {
        "Upi" => "UPI",
        "BankTransfer" => "Bank Transfer",
        _ => method
    };

    public static (string Label, string Colour) StatusStyle(string status) => status switch
    {
        "Paid" => ("PAID", "#15803d"),
        "Partial" => ("PARTLY PAID", "#b45309"),
        "Unpaid" => ("UNPAID", "#b91c1c"),
        "Settled" => ("SETTLED", "#475569"),
        _ => ("PAYMENT NOT RECORDED", "#475569")
    };

    public static IContainer GridCell(this IContainer container, bool header = false) =>
        header
            ? container.Background(Fill).BorderBottom(0.8f).BorderColor(Ink).PaddingVertical(3).PaddingHorizontal(3)
            : container.BorderBottom(0.4f).BorderColor(Rule).PaddingVertical(3).PaddingHorizontal(3);
}