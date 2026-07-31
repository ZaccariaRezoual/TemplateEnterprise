namespace EnterpriseFramework.Application.Exceptions;

/// <summary>
/// Thrown when an incoming request fails input validation
/// (raised automatically by the MediatR validation behavior from
/// FluentValidation results). Mapped by the API layer to HTTP 400 with a
/// per-field error dictionary in the ProblemDetails payload.
/// </summary>
public sealed class ValidationException : AppException
{
    /// <summary>
    /// Initializes the exception with the per-field validation failures.
    /// </summary>
    /// <param name="errors">
    /// Failed fields: key = property name, value = error messages for that property.
    /// </param>
    public ValidationException(IReadOnlyDictionary<string, string[]> errors)
        : base("One or more validation errors occurred.")
    {
        Errors = errors;
    }

    /// <summary>Failed fields: key = property name, value = error messages.</summary>
    public IReadOnlyDictionary<string, string[]> Errors { get; }
}
