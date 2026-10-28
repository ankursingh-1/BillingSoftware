using System.Text.Json;

namespace Billing.Application.Documents;

/// <summary>
/// The company and customer details exactly as they were when an invoice was created.
/// Stored with the sale so that editing Settings or a customer later never changes an
/// issued invoice. Only identity/header details are kept here; money, tax and quantities
/// are stored in their own columns.
/// </summary>
public class InvoiceSnapshot
{
    public int Version { get; set; } = 1;
    public SnapshotCompany Company { get; set; } = new();
    public SnapshotCustomer Customer { get; set; } = new();

    private static readonly JsonSerializerOptions Options = new() { WriteIndented = false };

    public string ToJson() => JsonSerializer.Serialize(this, Options);

    /// <summary>Returns null when there is no snapshot or it cannot be read (the caller falls back to current data).</summary>
    public static InvoiceSnapshot? TryParse(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
            return null;

        try
        {
            return JsonSerializer.Deserialize<InvoiceSnapshot>(json, Options);
        }
        catch (JsonException)
        {
            return null;
        }
    }
}

public class SnapshotCompany
{
    public string Name { get; set; } = string.Empty;
    public string? Address { get; set; }
    public string? City { get; set; }
    public string? State { get; set; }
    public string? StateCode { get; set; }
    public string? Pincode { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string? Website { get; set; }
    public string? Gstin { get; set; }
    public string? Pan { get; set; }

    /// <summary>Stored logo file name (the file is kept as long as an invoice may use it).</summary>
    public string? LogoFile { get; set; }

    public string? BankName { get; set; }
    public string? AccountNumber { get; set; }
    public string? IfscCode { get; set; }
    public string? UpiId { get; set; }
    public string? Terms { get; set; }
    public string? Footer { get; set; }
    public string Currency { get; set; } = "INR";
}

public class SnapshotCustomer
{
    public string Name { get; set; } = string.Empty;
    public string? Mobile { get; set; }
    public string? Email { get; set; }
    public string? Address { get; set; }
    public string? State { get; set; }
    public string? StateCode { get; set; }
    public string? Gstin { get; set; }
}