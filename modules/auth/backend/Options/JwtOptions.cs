namespace EnterpriseFramework.Modules.Auth.Options;

/// <summary>
/// Typed configuration for token issuance and validation (section "Jwt").
///
/// The HOST's JWT bearer validation reads the same section, so tokens issued
/// here are always verifiable by the pipeline — one source of truth.
/// </summary>
public sealed class JwtOptions
{
    /// <summary>Configuration section name.</summary>
    public const string SectionName = "Jwt";

    /// <summary>Token issuer (the API).</summary>
    public string Issuer { get; init; } = "enterprise-framework";

    /// <summary>Intended audience (the frontend applications).</summary>
    public string Audience { get; init; } = "enterprise-framework";

    /// <summary>
    /// HMAC-SHA256 signing key, at least 32 bytes. NEVER in source control:
    /// development uses appsettings.Development.json, production a secret
    /// store or environment variable. Startup fails fast when missing.
    /// </summary>
    public string SigningKey { get; init; } = string.Empty;

    /// <summary>
    /// Access token lifetime in minutes. Short by design: a stolen access
    /// token expires quickly, and sessions live on the refresh token.
    /// </summary>
    public int AccessTokenMinutes { get; init; } = 15;

    /// <summary>Refresh token lifetime in days (the maximum idle session length).</summary>
    public int RefreshTokenDays { get; init; } = 7;
}
