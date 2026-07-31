namespace EnterpriseFramework.Application.Exceptions;

/// <summary>
/// Thrown when a requested resource does not exist.
/// Mapped by the API layer to HTTP 404.
/// </summary>
public sealed class NotFoundException : AppException
{
    /// <summary>
    /// Initializes the exception for a missing resource.
    /// </summary>
    /// <param name="resource">Logical resource name (e.g. "User").</param>
    /// <param name="key">Identifier that was not found.</param>
    public NotFoundException(string resource, object key)
        : base($"{resource} with id '{key}' was not found.") { }
}
