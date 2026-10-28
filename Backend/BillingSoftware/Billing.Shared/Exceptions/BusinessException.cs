namespace Billing.Shared.Exceptions;

/// <summary>
/// An expected, user-correctable failure (validation, not found, conflict).
/// The message is safe to show to the client. Mapped to a 4xx response by
/// the global exception middleware.
/// </summary>
public class BusinessException : Exception
{
    public int StatusCode { get; }

    public BusinessException(string message, int statusCode = 400)
        : base(message)
    {
        StatusCode = statusCode;
    }

    public static BusinessException NotFound(string message) => new(message, 404);

    public static BusinessException Conflict(string message) => new(message, 409);
}
