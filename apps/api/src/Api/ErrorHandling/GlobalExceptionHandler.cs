using EnterpriseFramework.Api.Middleware;
using EnterpriseFramework.Application.Exceptions;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace EnterpriseFramework.Api.ErrorHandling;

/// <summary>
/// Single translation point from the shared application error hierarchy to
/// RFC 9457 ProblemDetails responses.
///
/// Mapping: ValidationException → 400 (+ "errors" dictionary),
/// UnauthorizedException → 401, ForbiddenException → 403,
/// NotFoundException → 404, BusinessException → 422.
/// Any other exception is a bug: logged and returned as a generic 500 with NO
/// internal details (no message, no stack trace) to avoid information leaks.
/// Every payload carries the request correlation id for support/debugging.
/// </summary>
public sealed partial class GlobalExceptionHandler : IExceptionHandler
{
    private readonly ILogger<GlobalExceptionHandler> _logger;

    [LoggerMessage(Level = LogLevel.Error, Message = "Unhandled exception")]
    private static partial void LogUnhandled(ILogger logger, Exception exception);

    /// <summary>
    /// Initializes the handler.
    /// </summary>
    /// <param name="logger">Logger for unexpected exceptions.</param>
    public GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Converts the exception into a ProblemDetails response.
    /// </summary>
    /// <param name="httpContext">Current HTTP context.</param>
    /// <param name="exception">Exception that escaped the pipeline.</param>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    /// <returns>Always <c>true</c>: every exception is handled here.</returns>
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken
    )
    {
        var problem = exception switch
        {
            ValidationException validation => new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Validation failed",
                Detail = validation.Message,
                Extensions = { ["errors"] = validation.Errors },
            },
            UnauthorizedException unauthorized => new ProblemDetails
            {
                Status = StatusCodes.Status401Unauthorized,
                Title = "Unauthorized",
                Detail = unauthorized.Message,
            },
            ForbiddenException forbidden => new ProblemDetails
            {
                Status = StatusCodes.Status403Forbidden,
                Title = "Forbidden",
                Detail = forbidden.Message,
            },
            NotFoundException notFound => new ProblemDetails
            {
                Status = StatusCodes.Status404NotFound,
                Title = "Resource not found",
                Detail = notFound.Message,
            },
            BusinessException business => new ProblemDetails
            {
                Status = StatusCodes.Status422UnprocessableEntity,
                Title = "Business rule violated",
                Detail = business.Message,
            },
            _ => new ProblemDetails
            {
                Status = StatusCodes.Status500InternalServerError,
                Title = "An unexpected error occurred",
                Detail = "The error has been logged. Contact support with the correlation id.",
            },
        };

        if (problem.Status == StatusCodes.Status500InternalServerError)
        {
            LogUnhandled(_logger, exception);
        }

        if (httpContext.Items[CorrelationIdMiddleware.HeaderName] is string correlationId)
        {
            problem.Extensions["correlationId"] = correlationId;
        }

        httpContext.Response.StatusCode =
            problem.Status ?? StatusCodes.Status500InternalServerError;
        await httpContext
            .Response.WriteAsJsonAsync(problem, cancellationToken)
            .ConfigureAwait(false);
        return true;
    }
}
