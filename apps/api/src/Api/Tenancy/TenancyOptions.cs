namespace EnterpriseFramework.Api.Tenancy;

/// <summary>How the tenant of a request is identified.</summary>
public enum TenantResolutionStrategy
{
    /// <summary>From a request header (default: "X-Tenant-Id").</summary>
    Header = 0,

    /// <summary>From the first label of the host name (acme.app.com → "acme").</summary>
    Subdomain = 1,

    /// <summary>From the "tenant" claim of the access token.</summary>
    Claim = 2,
}

/// <summary>
/// Typed configuration for multi-tenancy (section "Tenancy").
///
/// Disabled by default: multi-tenancy changes how every query behaves, so a
/// template must not impose it on projects that will never need it.
/// </summary>
public sealed class TenancyOptions
{
    /// <summary>Configuration section name.</summary>
    public const string SectionName = "Tenancy";

    /// <summary>Claim type carrying the tenant a user belongs to.</summary>
    public const string TenantClaimType = "tenant";

    /// <summary>Whether multi-tenancy is active. Off by default.</summary>
    public bool Enabled { get; init; }

    /// <summary>How the tenant is identified.</summary>
    public TenantResolutionStrategy Strategy { get; init; } = TenantResolutionStrategy.Header;

    /// <summary>Header carrying the tenant when using the header strategy.</summary>
    public string HeaderName { get; init; } = "X-Tenant-Id";

    /// <summary>
    /// Paths that never carry a tenant (sign-in, health, OpenAPI). Requests
    /// to them are allowed through unresolved; anything else is rejected when
    /// tenancy is enabled, because a request that silently loses its tenant
    /// would read across the whole installation.
    /// </summary>
    public IReadOnlyList<string> TenantlessPaths { get; init; } =
        ["/health", "/openapi", "/api/auth", "/api/localization"];
}
