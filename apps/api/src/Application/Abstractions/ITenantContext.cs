namespace EnterpriseFramework.Application.Abstractions;

/// <summary>
/// The tenant the current request belongs to.
///
/// Application code and modules depend on this instead of reading headers or
/// host names, so how a tenant is identified (header, subdomain, token claim)
/// can change without touching a single feature.
///
/// When multi-tenancy is DISABLED — the default — <see cref="TenantId"/> is
/// null and tenant query filters are inert, so a single-tenant deployment
/// carries no tenancy behaviour at all.
/// </summary>
public interface ITenantContext
{
    /// <summary>Whether multi-tenancy is enabled for this deployment.</summary>
    bool IsMultiTenant { get; }

    /// <summary>
    /// Tenant of the current request, or null when multi-tenancy is disabled
    /// or the request has no tenant (e.g. sign-in, health checks).
    /// </summary>
    Guid? TenantId { get; }

    /// <summary>
    /// Tenant of the current request.
    /// </summary>
    /// <returns>The resolved tenant.</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when no tenant is resolved. Use it where a tenant is mandatory:
    /// failing loudly beats writing a row with an empty tenant that then
    /// belongs to nobody and shows up for everybody.
    /// </exception>
    Guid RequireTenantId();
}
