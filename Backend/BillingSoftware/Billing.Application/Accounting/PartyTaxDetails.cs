namespace Billing.Application.Accounting;

/// <summary>Validated, canonical state / state code / GSTIN of a customer, supplier or company.</summary>
public readonly record struct PartyTaxInfo(string? State, string? StateCode, string? Gstin);

/// <summary>Thrown for user-correctable problems with state/GSTIN input.</summary>
public class PartyTaxException : Exception
{
    public PartyTaxException(string message) : base(message) { }
}

public static class PartyTaxDetails
{
    /// <summary>
    /// Normalises and cross-checks the three fields:
    ///  - GSTIN is upper-cased and must have a valid format;
    ///  - the state code is taken from, in order, the explicit code, the GSTIN prefix, the state name;
    ///  - the state name is replaced by the official name for that code;
    ///  - a GSTIN whose prefix disagrees with an explicitly chosen state is rejected.
    /// All three fields are optional (a walk-in customer needs none of them).
    /// </summary>
    public static PartyTaxInfo Normalize(string? state, string? stateCode, string? gstin)
    {
        var gstinValue = GstStates.NormalizeGstin(gstin);

        if (gstinValue != null && !GstStates.IsValidGstinFormat(gstinValue))
            throw new PartyTaxException("Enter a valid 15-character GSTIN (for example 27AAPFU0939F1ZV).");

        var stateName = string.IsNullOrWhiteSpace(state) ? null : state.Trim();
        var codeValue = string.IsNullOrWhiteSpace(stateCode) ? null : stateCode.Trim();

        if (codeValue != null && !GstStates.IsValidCode(codeValue))
            throw new PartyTaxException("Unknown GST state code.");

        var fromName = GstStates.CodeFromName(stateName);

        if (stateName != null && fromName == null && codeValue == null && gstinValue == null)
            throw new PartyTaxException("Unrecognised state. Select a state from the list.");

        var fromGstin = GstStates.CodeFromGstin(gstinValue);

        var explicitCode = codeValue ?? fromName;

        if (gstinValue != null && explicitCode != null && fromGstin != explicitCode)
        {
            throw new PartyTaxException(
                $"The GSTIN belongs to {GstStates.NameOf(fromGstin)} ({fromGstin}), " +
                $"but the selected state is {GstStates.NameOf(explicitCode)} ({explicitCode}).");
        }

        var resolved = explicitCode ?? fromGstin;

        return new PartyTaxInfo(
            resolved != null ? GstStates.NameOf(resolved) : null,
            resolved,
            gstinValue);
    }
}
