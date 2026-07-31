namespace EnterpriseFramework.Api.Options;

/// <summary>
/// Typed configuration for the default CORS policy (section "Cors").
/// The API refuses cross-origin calls from origins not listed here.
/// </summary>
public sealed class CorsOptions
{
    /// <summary>Configuration section name.</summary>
    public const string SectionName = "Cors";

    /// <summary>
    /// Origins allowed to call the API (e.g. "http://localhost:5173").
    /// Empty list = no cross-origin access (safe default).
    /// </summary>
    public IReadOnlyList<string> AllowedOrigins { get; init; } = [];
}
