namespace EnterpriseFramework.Application.Exceptions;

/// <summary>
/// Thrown when the caller is authenticated but lacks the required
/// role/permission (RBAC/PBAC). Mapped by the API layer to HTTP 403.
/// </summary>
public sealed class ForbiddenException : AppException
{
    /// <summary>
    /// Initializes the exception.
    /// </summary>
    /// <param name="message">Client-safe description; defaults to a generic message.</param>
    public ForbiddenException(string message = "You do not have permission to perform this action.")
        : base(message) { }
}
