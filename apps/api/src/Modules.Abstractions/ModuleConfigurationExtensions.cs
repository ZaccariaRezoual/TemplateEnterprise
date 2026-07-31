using Microsoft.Extensions.Configuration;

namespace EnterpriseFramework.Modules.Abstractions;

/// <summary>
/// Configuration helpers shared by every module, so settings are read the same
/// way everywhere instead of each module inventing its own key convention.
/// </summary>
public static class ModuleConfigurationExtensions
{
    /// <summary>
    /// Tells whether a module should apply its pending migrations at startup.
    ///
    /// Resolution order: the module's own "Modules:{name}:AutoMigrate", then
    /// the host-wide "Modules:AutoMigrate" default (true in development only).
    /// The layering matters: a module can opt out in development, and no
    /// module can accidentally migrate a production database on boot.
    /// </summary>
    /// <param name="configuration">Application configuration.</param>
    /// <param name="moduleName">Name of the module asking.</param>
    /// <returns><c>true</c> when the module should migrate on startup.</returns>
    public static bool ShouldAutoMigrate(this IConfiguration configuration, string moduleName)
    {
        var hostDefault = configuration.GetValue("Modules:AutoMigrate", defaultValue: false);
        return configuration.GetValue($"Modules:{moduleName}:AutoMigrate", hostDefault);
    }
}
