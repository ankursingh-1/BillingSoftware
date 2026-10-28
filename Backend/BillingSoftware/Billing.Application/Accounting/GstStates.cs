using System.Text.RegularExpressions;

namespace Billing.Application.Accounting;

/// <summary>Indian states / union territories and their two-digit GST state codes.</summary>
public static class GstStates
{
    public static readonly IReadOnlyList<(string Code, string Name)> All = new (string, string)[]
    {
        ("01", "Jammu and Kashmir"), ("02", "Himachal Pradesh"), ("03", "Punjab"), ("04", "Chandigarh"),
        ("05", "Uttarakhand"), ("06", "Haryana"), ("07", "Delhi"), ("08", "Rajasthan"),
        ("09", "Uttar Pradesh"), ("10", "Bihar"), ("11", "Sikkim"), ("12", "Arunachal Pradesh"),
        ("13", "Nagaland"), ("14", "Manipur"), ("15", "Mizoram"), ("16", "Tripura"),
        ("17", "Meghalaya"), ("18", "Assam"), ("19", "West Bengal"), ("20", "Jharkhand"),
        ("21", "Odisha"), ("22", "Chhattisgarh"), ("23", "Madhya Pradesh"), ("24", "Gujarat"),
        ("26", "Dadra and Nagar Haveli and Daman and Diu"), ("27", "Maharashtra"), ("29", "Karnataka"),
        ("30", "Goa"), ("31", "Lakshadweep"), ("32", "Kerala"), ("33", "Tamil Nadu"),
        ("34", "Puducherry"), ("35", "Andaman and Nicobar Islands"), ("36", "Telangana"),
        ("37", "Andhra Pradesh"), ("38", "Ladakh"), ("97", "Other Territory"),
    };

    private static readonly Regex GstinFormat = new(
        "^[0-9]{2}[A-Z]{5}[0-9]{4}[A-Z][1-9A-Z]Z[0-9A-Z]$",
        RegexOptions.Compiled);

    private static readonly Dictionary<string, string> NameToCode = BuildNameIndex();

    private static Dictionary<string, string> BuildNameIndex()
    {
        var map = new Dictionary<string, string>();
        foreach (var (code, name) in All)
            map[Normalize(name)] = code;

        // Common alternative spellings.
        map[Normalize("Orissa")] = "21";
        map[Normalize("Pondicherry")] = "34";
        map[Normalize("Jammu & Kashmir")] = "01";
        map[Normalize("Andaman & Nicobar")] = "35";
        map[Normalize("Daman and Diu")] = "26";
        map[Normalize("Dadra and Nagar Haveli")] = "26";
        map[Normalize("NCT of Delhi")] = "07";
        map[Normalize("New Delhi")] = "07";
        return map;
    }

    private static string Normalize(string value) =>
        Regex.Replace(value.ToLowerInvariant().Replace("&", " and "), "[^a-z]+", " ").Trim();

    public static bool IsValidCode(string? code) =>
        code != null && All.Any(s => s.Code == code);

    public static string? NameOf(string? code) =>
        All.Where(s => s.Code == code).Select(s => s.Name).FirstOrDefault();

    public static string? CodeFromName(string? name) =>
        string.IsNullOrWhiteSpace(name) ? null
        : NameToCode.TryGetValue(Normalize(name), out var code) ? code : null;

    public static bool IsValidGstinFormat(string? gstin) =>
        gstin != null && GstinFormat.IsMatch(gstin) && IsValidCode(gstin[..2]);

    /// <summary>First two digits of a GSTIN are the state code.</summary>
    public static string? CodeFromGstin(string? gstin) =>
        IsValidGstinFormat(gstin) ? gstin![..2] : null;

    /// <summary>Upper-case, trimmed; null for empty.</summary>
    public static string? NormalizeGstin(string? gstin)
    {
        var value = gstin?.Trim().ToUpperInvariant();
        return string.IsNullOrEmpty(value) ? null : value;
    }
}
