namespace Billing.Application.Accounting;

/// <summary>
/// Decides which GST state a party is in and whether a transaction is inter-state.
///
/// Provisional default (documented, to be confirmed by the business): a customer or
/// supplier with no usable state information is treated as being in the company's own
/// state, so the transaction is intra-state (CGST + SGST).
/// </summary>
public static class PlaceOfSupply
{
    /// <summary>Company state: explicit code, else the state name, else the GSTIN prefix.</summary>
    public static string? CompanyStateCode(string? stateCode, string? stateName, string? gstin) =>
        ValidCode(stateCode)
        ?? GstStates.CodeFromName(stateName)
        ?? GstStates.CodeFromGstin(GstStates.NormalizeGstin(gstin));

    /// <summary>Party state: explicit code, else the GSTIN prefix, else the state name.</summary>
    public static string? PartyStateCode(string? stateCode, string? stateName, string? gstin) =>
        ValidCode(stateCode)
        ?? GstStates.CodeFromGstin(GstStates.NormalizeGstin(gstin))
        ?? GstStates.CodeFromName(stateName);

    /// <summary>
    /// State used for the invoice: the party's state, or the company state when the party's
    /// state is unknown (the documented provisional default).
    /// </summary>
    public static string? Resolve(string? partyStateCode, string? companyStateCode) =>
        partyStateCode ?? companyStateCode;

    public static bool IsInterState(string? placeOfSupplyCode, string? companyStateCode) =>
        placeOfSupplyCode != null && companyStateCode != null && placeOfSupplyCode != companyStateCode;

    private static string? ValidCode(string? code)
    {
        var value = code?.Trim();
        return GstStates.IsValidCode(value) ? value : null;
    }
}
