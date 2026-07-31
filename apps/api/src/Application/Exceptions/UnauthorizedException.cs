namespace EnterpriseFramework.Application.Exceptions;

/// <summary>
/// Thrown when the caller is not authenticated (missing/invalid credentials).
/// Mapped by the API layer to HTTP 401.
/// </summary>
public sealed class UnauthorizedException : AppException
{
    /// <summary>
    /// Initializes the exception.
    /// </summary>
    /// <param name="message">Client-safe description; defaults to a generic message.</param>
    public UnauthorizedException(string message = "Authentication is required.")
        : base(message) { }
}
