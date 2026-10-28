namespace Billing.Application.Interfaces;

public interface IAuditService
{
    /// <summary>
    /// Adds an audit entry to the current unit of work WITHOUT saving, so it is
    /// committed (or rolled back) together with the business change it describes.
    /// </summary>
    void Record(string action, string? entityType = null, string? entityId = null,
        string? details = null, string? actorEmail = null);

    /// <summary>Records and saves immediately (for events with no business transaction, e.g. login).</summary>
    Task LogAsync(string action, string? entityType = null, string? entityId = null,
        string? details = null, string? actorEmail = null);
}
