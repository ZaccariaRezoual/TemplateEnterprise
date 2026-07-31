namespace EnterpriseFramework.Api.Options;

/// <summary>
/// Typed configuration for the global fixed-window rate limiter
/// (section "RateLimiting"), partitioned per client IP.
/// </summary>
public sealed class RateLimitingOptions
{
    /// <summary>Configuration section name.</summary>
    public const string SectionName = "RateLimiting";

    /// <summary>Maximum requests allowed per window per client IP.</summary>
    public int PermitLimit { get; init; } = 100;

    /// <summary>Window length in seconds.</summary>
    public int WindowSeconds { get; init; } = 60;
}
