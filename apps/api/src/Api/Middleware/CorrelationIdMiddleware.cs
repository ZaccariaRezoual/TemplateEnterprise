using Serilog.Context;

namespace EnterpriseFramework.Api.Middleware;

/// <summary>
/// Assigns a correlation id to every request and flows it through logs and
/// responses, so a single user action can be traced across API, logs (Seq)
/// and, later, distributed services.
///
/// Behavior: reuses the incoming "X-Correlation-ID" header when present
/// (trusted callers/gateways), otherwise generates a new GUID. The id is
/// pushed into the Serilog <see cref="LogContext"/> and echoed back in the
/// response header.
/// </summary>
public sealed class CorrelationIdMiddleware
{
    /// <summary>Header used to receive and expose the correlation id.</summary>
    public const string HeaderName = "X-Correlation-ID";

    private readonly RequestDelegate _next;

    /// <summary>
    /// Initializes the middleware.
    /// </summary>
    /// <param name="next">Next delegate in the pipeline.</param>
    public CorrelationIdMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    /// <summary>
    /// Resolves the correlation id, enriches the log context and continues the pipeline.
    /// </summary>
    /// <param name="context">Current HTTP context.</param>
    /// <returns>A task completing when the pipeline finishes.</returns>
    public async Task InvokeAsync(HttpContext context)
    {
        var correlationId = context.Request.Headers.TryGetValue(HeaderName, out var incoming)
            && !string.IsNullOrWhiteSpace(incoming)
                ? incoming.ToString()
                : Guid.NewGuid().ToString("N");

        context.Items[HeaderName] = correlationId;
        context.Response.Headers[HeaderName] = correlationId;

        using (LogContext.PushProperty("CorrelationId", correlationId))
        {
            await _next(context).ConfigureAwait(false);
        }
    }
}
