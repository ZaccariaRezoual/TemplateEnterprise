namespace EnterpriseFramework.Application.Exceptions;

/// <summary>
/// Thrown when a request is well-formed but violates a business rule
/// (e.g. "cannot delete the last administrator").
/// Mapped by the API layer to HTTP 422.
/// </summary>
public sealed class BusinessException : AppException
{
    /// <summary>
    /// Initializes the exception with the violated business rule.
    /// </summary>
    /// <param name="message">Client-safe description of the violated rule.</param>
    public BusinessException(string message)
        : base(message) { }
}
