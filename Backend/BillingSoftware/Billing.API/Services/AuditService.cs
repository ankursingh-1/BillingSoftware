using Billing.Application.Interfaces;
using Billing.Domain.Entities;
using Billing.Persistence.Context;

namespace Billing.API.Services;

public class AuditService : IAuditService
{
    private const int MaxDetails = 2000;

    private readonly BillingDbContext _context;
    private readonly ICurrentUser _user;

    public AuditService(BillingDbContext context, ICurrentUser user)
    {
        _context = context;
        _user = user;
    }

    public void Record(string action, string? entityType = null, string? entityId = null,
        string? details = null, string? actorEmail = null)
    {
        _context.AuditLogs.Add(new AuditLog
        {
            OccurredOn = DateTime.UtcNow,
            UserId = _user.UserId,
            UserEmail = Truncate(actorEmail ?? _user.Email, 200),
            Action = Truncate(action, 100)!,
            EntityType = Truncate(entityType, 100),
            EntityId = Truncate(entityId, 50),
            Details = Truncate(details, MaxDetails),
            IpAddress = Truncate(_user.IpAddress, 45)
        });
    }

    public async Task LogAsync(string action, string? entityType = null, string? entityId = null,
        string? details = null, string? actorEmail = null)
    {
        Record(action, entityType, entityId, details, actorEmail);
        await _context.SaveChangesAsync();
    }

    private static string? Truncate(string? value, int max) =>
        value is { Length: > 0 } && value.Length > max ? value[..max] : value;
}
