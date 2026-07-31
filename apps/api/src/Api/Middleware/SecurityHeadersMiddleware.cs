namespace EnterpriseFramework.Api.Middleware;

/// <summary>
/// Adds the baseline security headers to every response ("secure by default"
/// rule: every project starts protected, hardening is not deferred).
///
/// Headers are conservative defaults for a JSON API; a project serving HTML
/// should extend them (e.g. Content-Security-Policy) in its own composition root.
/// </summary>
public sealed class SecurityHeadersMiddleware
{
    private readonly RequestDelegate _next;

    /// <summary>
    /// Initializes the middleware.
    /// </summary>
    /// <param name="next">Next delegate in the pipeline.</param>
    public SecurityHeadersMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    /// <summary>
    /// Sets the security headers and continues the pipeline.
    /// </summary>
    /// <param name="context">Current HTTP context.</param>
    /// <returns>A task completing when the pipeline finishes.</returns>
    public Task InvokeAsync(HttpContext context)
    {
        var headers = context.Response.Headers;
        headers["X-Content-Type-Options"] = "nosniff";
        headers["X-Frame-Options"] = "DENY";
        headers["Referrer-Policy"] = "no-referrer";
        headers["Cross-Origin-Opener-Policy"] = "same-origin";
        headers["Cross-Origin-Resource-Policy"] = "same-site";
        headers["Permissions-Policy"] = "camera=(), geolocation=(), microphone=()";
        return _next(context);
    }
}
