namespace EnterpriseFramework.Api.Features;

/// <summary>
/// Definition of one feature flag (section "Features:Flags").
/// </summary>
public sealed class FeatureFlagDefinition
{
    /// <summary>Whether the capability is on for everyone not listed below.</summary>
    public bool Enabled { get; init; }

    /// <summary>
    /// Tenants the flag is on for regardless of <see cref="Enabled"/>.
    /// Empty means "no exceptions", which is the common case.
    /// </summary>
    public IReadOnlyList<Guid> EnabledForTenants { get; init; } = [];

    /// <summary>
    /// Accounts the flag is on for regardless of <see cref="Enabled"/>.
    /// Useful for letting the team try something before customers do.
    /// </summary>
    public IReadOnlyList<Guid> EnabledForUsers { get; init; } = [];
}

/// <summary>
/// Typed configuration for feature flags (section "Features").
///
/// Configuration-backed on purpose: a flag change is then a deployment
/// artifact, reviewable and revertible like any other, rather than a live
/// edit nobody can trace afterwards. Projects needing runtime toggles replace
/// the provider, not the callers.
/// </summary>
public sealed class FeatureFlagOptions
{
    /// <summary>Configuration section name.</summary>
    public const string SectionName = "Features";

    /// <summary>Declared flags, by name.</summary>
    public IReadOnlyDictionary<string, FeatureFlagDefinition> Flags { get; init; } =
        new Dictionary<string, FeatureFlagDefinition>(StringComparer.OrdinalIgnoreCase);
}
