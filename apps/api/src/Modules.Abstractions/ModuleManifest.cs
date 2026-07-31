namespace EnterpriseFramework.Modules.Abstractions;

/// <summary>
/// Deserialized content of a module's <c>module.json</c> manifest
/// (embedded in the module assembly with logical name "module.json").
///
/// The manifest is the source of truth for identity, version and dependencies;
/// the "Enabled" flag can be overridden per environment via the
/// "Modules:{Name}:Enabled" configuration key.
/// </summary>
public sealed record ModuleManifest
{
    /// <summary>Unique module name (e.g. "Demo", "Auth").</summary>
    public required string Name { get; init; }

    /// <summary>Semantic version of the module (e.g. "1.0.0").</summary>
    public required string Version { get; init; }

    /// <summary>Names of the modules this module depends on; they are loaded first.</summary>
    public IReadOnlyList<string> Dependencies { get; init; } = [];

    /// <summary>Whether the module is active by default (configuration can override).</summary>
    public bool Enabled { get; init; } = true;
}
