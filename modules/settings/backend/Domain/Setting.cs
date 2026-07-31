using EnterpriseFramework.Domain.Common;

namespace EnterpriseFramework.Modules.Settings.Domain;

/// <summary>
/// A stored setting value.
///
/// Settings resolve in layers: a per-user value overrides the global one,
/// which overrides the default declared in code. Storing only what was
/// explicitly set (rather than a row per user per key) keeps the table small
/// and makes "change the default for everyone" a code change, not a migration.
/// </summary>
public sealed class Setting : EntityBase<Guid>
{
    private Setting() { }

    /// <summary>Dotted key, e.g. "ui.itemsPerPage".</summary>
    public string Key { get; private set; } = string.Empty;

    /// <summary>
    /// Account this value belongs to, or null for the global value.
    /// </summary>
    public Guid? UserId { get; private set; }

    /// <summary>Raw value, serialized as text and parsed by the typed accessor.</summary>
    public string Value { get; private set; } = string.Empty;

    /// <summary>UTC instant of the last write.</summary>
    public DateTime UpdatedAtUtc { get; private set; }

    /// <summary>
    /// Creates a setting value.
    /// </summary>
    /// <param name="key">Setting key.</param>
    /// <param name="value">Raw value.</param>
    /// <param name="userId">Owner, or null for the global scope.</param>
    /// <returns>The new value.</returns>
    public static Setting Create(string key, string value, Guid? userId) =>
        new()
        {
            Id = Guid.NewGuid(),
            Key = key,
            Value = value,
            UserId = userId,
            UpdatedAtUtc = DateTime.UtcNow,
        };

    /// <summary>
    /// Replaces the stored value.
    /// </summary>
    /// <param name="value">The new raw value.</param>
    public void Update(string value)
    {
        Value = value;
        UpdatedAtUtc = DateTime.UtcNow;
    }
}
