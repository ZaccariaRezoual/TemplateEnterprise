namespace EnterpriseFramework.Application.Exceptions;

/// <summary>
/// Root of the shared application error hierarchy
/// (mirrored on the frontend as <c>ApplicationError</c>).
///
/// Every expected failure thrown by application code derives from this class
/// so the API layer can translate it into an RFC 9457 ProblemDetails response.
/// Unexpected exceptions (bugs) must NOT derive from it: they surface as 500.
/// </summary>
public abstract class AppException : Exception
{
    /// <summary>
    /// Initializes the exception with a human-readable, non-sensitive message
    /// (it is returned to API clients).
    /// </summary>
    /// <param name="message">Description of the failure, safe for clients.</param>
    protected AppException(string message)
        : base(message) { }
}
