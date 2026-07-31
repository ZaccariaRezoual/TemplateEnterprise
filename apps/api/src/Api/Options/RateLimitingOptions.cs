namespace EnterpriseFramework.Api.Options;

/// <summary>
/// Typed configuration for the global fixed-window rate limiter
/// (section "RateLimiting"), partitioned per client IP.
/// </summary>
public sealed class RateLimitingOptions
{
    /// <summary>Configuration section name.</summary>
    public const string SectionName = "RateLimiting";

    /// <summary>
    /// Maximum requests allowed per window per client IP.
    ///
    /// Partitioning by IP means everyone behind one NAT — an office, a
    /// campus, a mobile carrier — shares this budget, and a single-page app
    /// makes several calls per screen. The default is therefore generous
    /// rather than tight: it is a blunt guard against runaway clients, not
    /// the application's authorization layer. Tune it per deployment, and
    /// prefer per-endpoint policies (see the Auth module) for anything that
    /// actually needs a strict limit.
    /// </summary>
    public int PermitLimit { get; init; } = 600;

    /// <summary>Window length in seconds.</summary>
    public int WindowSeconds { get; init; } = 60;
}
