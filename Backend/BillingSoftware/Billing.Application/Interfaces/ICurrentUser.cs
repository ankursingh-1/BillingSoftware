namespace Billing.Application.Interfaces;

/// <summary>The authenticated user making the current request (null values when anonymous).</summary>
public interface ICurrentUser
{
    int? UserId { get; }
    string? Email { get; }
    string? Role { get; }
    string? IpAddress { get; }
}
