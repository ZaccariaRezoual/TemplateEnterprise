using System.Security.Claims;
using EnterpriseFramework.Application.Abstractions;
using Microsoft.Extensions.Options;
using Serilog.Context;

namespace EnterpriseFramework.Api.Tenancy;

/// <summary>
/// Resolves the tenant of each request and rejects requests that should have
/// one but do not.
///
/// Runs AFTER authentication, because the claim strategy needs a principal
/// and because a client-supplied tenant must be checked against the tenant
/// the user actually belongs to. Without that check, multi-tenancy is a
/// suggestion: anyone could read another customer's data by editing a header.
///
/// Does nothing at all when multi-tenancy is disabled.
/// </summary>
public sealed partial class TenantResolutionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly TenancyOptions _options;
    private readonly ILogger<TenantResolutionMiddleware> _logger;

    [LoggerMessage(
        Level = LogLevel.Warning,
        Message = "Request for tenant {RequestedTenant} rejected: caller belongs to {UserTenant}"
    )]
    private static partial void LogTenantMismatch(
        ILogger logger,
        Guid requestedTenant,
        string userTenant
    );

    /// <summary>
    /// Initializes the middleware.
    /// </summary>
    /// <param name="next">Next delegate in the pipeline.</param>
    /// <param name="options">Tenancy configuration.</param>
    /// <param name="logger">Reports rejected cross-tenant attempts.</param>
    public TenantResolutionMiddleware(
        RequestDelegate next,
        IOptions<TenancyOptions> options,
        ILogger<TenantResolutionMiddleware> logger
    )
    {
        _next = next;
        _options = options.Value;
        _logger = logger;
    }

    /// <summary>
    /// Resolves the tenant, validates it against the caller and continues.
    /// </summary>
    /// <param name="context">Current HTTP context.</param>
    /// <param name="tenantContext">Scoped context to fill.</param>
    /// <returns>A task completing when the pipeline finishes.</returns>
    public async Task InvokeAsync(HttpContext context, ITenantContext tenantContext)
    {
        if (!_options.Enabled)
        {
            await _next(context).ConfigureAwait(false);
            return;
        }

        var resolved = Resolve(context);

        if (resolved is null)
        {
            if (IsTenantless(context.Request.Path))
            {
                await _next(context).ConfigureAwait(false);
                return;
            }

            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            await context
                .Response.WriteAsJsonAsync(
                    new { title = "Tenant required", detail = "This request carries no tenant." }
                )
                .ConfigureAwait(false);
            return;
        }

        // The decisive check: a tenant asserted by the client must match the
        // one the token says the user belongs to.
        var userTenant = context.User.FindFirstValue(TenancyOptions.TenantClaimType);
        if (
            context.User.Identity?.IsAuthenticated == true
            && (!Guid.TryParse(userTenant, out var claimTenant) || claimTenant != resolved.Value)
        )
        {
            LogTenantMismatch(_logger, resolved.Value, userTenant ?? "<none>");
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            return;
        }

        ((TenantContext)tenantContext).Resolve(resolved.Value);

        using (LogContext.PushProperty("TenantId", resolved.Value))
        {
            await _next(context).ConfigureAwait(false);
        }
    }

    private Guid? Resolve(HttpContext context) =>
        _options.Strategy switch
        {
            TenantResolutionStrategy.Header => Parse(context.Request.Headers[_options.HeaderName]),
            TenantResolutionStrategy.Subdomain => Parse(
                context.Request.Host.Host.Split('.') is [var label, _, ..] ? label : null
            ),
            TenantResolutionStrategy.Claim => Parse(
                context.User.FindFirstValue(TenancyOptions.TenantClaimType)
            ),
            _ => null,
        };

    private static Guid? Parse(string? value) =>
        Guid.TryParse(value, out var tenantId) ? tenantId : null;

    private bool IsTenantless(PathString path) =>
        _options.TenantlessPaths.Any(prefix =>
            path.StartsWithSegments(prefix, StringComparison.OrdinalIgnoreCase)
        );
}
