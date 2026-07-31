namespace EnterpriseFramework.Modules.Settings.Domain;

/// <summary>Where a setting may be overridden.</summary>
public enum SettingScope
{
    /// <summary>One value for the whole installation; only administrators change it.</summary>
    Global = 0,

    /// <summary>Each account may override the global value for itself.</summary>
    User = 1,
}

/// <summary>
/// Declaration of a setting: its key, default, scope and type.
///
/// Settings are DECLARED IN CODE rather than created at runtime. That is what
/// makes them typed, discoverable and documented — a free-form key/value store
/// becomes an undocumented API within a release, and nobody can tell which
/// keys still matter.
/// </summary>
/// <param name="Key">Dotted key, e.g. "ui.itemsPerPage".</param>
/// <param name="Description">What the setting controls, shown in the admin UI.</param>
/// <param name="Scope">Where it may be overridden.</param>
/// <param name="DefaultValue">Value used when nothing is stored.</param>
/// <param name="ValueType">Type the raw value is parsed into.</param>
public sealed record SettingDefinition(
    string Key,
    string Description,
    SettingScope Scope,
    string DefaultValue,
    SettingValueType ValueType
);

/// <summary>Types a setting value can take.</summary>
public enum SettingValueType
{
    /// <summary>Free text.</summary>
    Text = 0,

    /// <summary>Whole number.</summary>
    Number = 1,

    /// <summary>True/false.</summary>
    Flag = 2,
}

/// <summary>
/// Catalogue of settings the framework ships with. Modules add their own
/// entries here (or contribute them at registration, once a module
/// marketplace exists in Fase 7).
/// </summary>
public static class SettingKeys
{
    /// <summary>Rows shown per page in list views.</summary>
    public const string ItemsPerPage = "ui.itemsPerPage";

    /// <summary>Whether users receive notification emails.</summary>
    public const string EmailNotifications = "notifications.email";

    /// <summary>Public name of the installation, shown in emails and titles.</summary>
    public const string ApplicationName = "app.name";

    /// <summary>Every declared setting, used for validation and the admin UI.</summary>
    public static IReadOnlyList<SettingDefinition> All { get; } =
        [
            new(
                ItemsPerPage,
                "Rows shown per page in list views.",
                SettingScope.User,
                "25",
                SettingValueType.Number
            ),
            new(
                EmailNotifications,
                "Send notification emails to users.",
                SettingScope.User,
                "true",
                SettingValueType.Flag
            ),
            new(
                ApplicationName,
                "Public name of this installation.",
                SettingScope.Global,
                "Enterprise Framework",
                SettingValueType.Text
            ),
        ];

    /// <summary>
    /// Finds a declaration by key.
    /// </summary>
    /// <param name="key">Setting key.</param>
    /// <returns>The declaration, or null when the key is unknown.</returns>
    public static SettingDefinition? Find(string key) =>
        All.FirstOrDefault(definition => definition.Key == key);
}
