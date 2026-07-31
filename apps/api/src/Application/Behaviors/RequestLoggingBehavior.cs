using System.Diagnostics;
using MediatR;
using Microsoft.Extensions.Logging;

namespace EnterpriseFramework.Application.Behaviors;

/// <summary>
/// MediatR pipeline behavior that logs every use case execution with its
/// duration, giving uniform observability of the application layer without
/// any logging code inside handlers.
///
/// Logs request NAME only, never the payload: request bodies may contain
/// sensitive data and structured payload logging is an explicit opt-in.
/// Uses <see cref="LoggerMessage"/> delegates (CA1848): zero allocation when
/// the log level is disabled.
/// </summary>
/// <typeparam name="TRequest">MediatR request type.</typeparam>
/// <typeparam name="TResponse">Handler response type.</typeparam>
public sealed class RequestLoggingBehavior<TRequest, TResponse>
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private static readonly Action<ILogger, string, long, Exception?> LogHandled =
        LoggerMessage.Define<string, long>(
            LogLevel.Information,
            new EventId(1, "RequestHandled"),
            "Handled {RequestName} in {ElapsedMilliseconds}ms"
        );

    private static readonly Action<ILogger, string, long, Exception?> LogFailed =
        LoggerMessage.Define<string, long>(
            LogLevel.Warning,
            new EventId(2, "RequestFailed"),
            "Failed {RequestName} after {ElapsedMilliseconds}ms"
        );

    private readonly ILogger<RequestLoggingBehavior<TRequest, TResponse>> _logger;

    /// <summary>
    /// Initializes the behavior.
    /// </summary>
    /// <param name="logger">Logger scoped to this behavior.</param>
    public RequestLoggingBehavior(ILogger<RequestLoggingBehavior<TRequest, TResponse>> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Invokes the pipeline measuring elapsed time; failures are logged and re-thrown.
    /// </summary>
    /// <param name="request">Incoming request.</param>
    /// <param name="next">Continuation to the handler (or next behavior).</param>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    /// <returns>The handler response.</returns>
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken
    )
    {
        var requestName = typeof(TRequest).Name;
        var stopwatch = Stopwatch.StartNew();
        try
        {
            var response = await next().ConfigureAwait(false);
            LogHandled(_logger, requestName, stopwatch.ElapsedMilliseconds, null);
            return response;
        }
        catch (Exception exception)
        {
            LogFailed(_logger, requestName, stopwatch.ElapsedMilliseconds, exception);
            throw;
        }
    }
}
