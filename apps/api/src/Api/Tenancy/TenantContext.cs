using EnterpriseFramework.Application.Abstractions;
using Microsoft.Extensions.Options;

namespace EnterpriseFramework.Api.Tenancy;

/// <summary>
/// Scoped <see cref="ITenantContext"/> filled by <see cref="TenantResolutionMiddleware"/>.
///
/// Deliberately write-once: the tenant is decided at the edge of the request
/// and cannot be changed afterwards. A service able to switch tenant
/// mid-request is the shape every cross-tenant leak takes.
/// </summary>
public sealed class TenantContext : ITenantContext
{
    private readonly TenancyOptions _options;

    /// <summary>
    /// Initializes the context.
    /// </summary>
    /// <param name="options">Tenancy configuration.</param>
    public TenantContext(IOptions<TenancyOptions> options)
    {
        _options = options.Value;
    }

    /// <inheritdoc />
    public bool IsMultiTenant => _options.Enabled;

    /// <inheritdoc />
    public Guid? TenantId { get; private set; }

    /// <inheritdoc />
    public Guid RequireTenantId() =>
        TenantId
        ?? throw new InvalidOperationException(
            "No tenant is resolved for the current request. "
                + "Either the endpoint is tenantless or multi-tenancy is disabled."
        );

    /// <summary>
    /// Sets the tenant for this request. Called once, by the resolution
    /// middleware, before anything else runs.
    /// </summary>
    /// <param name="tenantId">The resolved tenant.</param>
    /// <exception cref="InvalidOperationException">Thrown on a second call.</exception>
    internal void Resolve(Guid tenantId)
    {
        if (TenantId is not null)
        {
            throw new InvalidOperationException("The tenant of a request cannot be reassigned.");
        }

        TenantId = tenantId;
    }
}
