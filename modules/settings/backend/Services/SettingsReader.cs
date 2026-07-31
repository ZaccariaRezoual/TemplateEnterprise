using System.Globalization;
using EnterpriseFramework.Modules.Settings.Domain;
using EnterpriseFramework.Modules.Settings.Persistence;
using Microsoft.EntityFrameworkCore;

namespace EnterpriseFramework.Modules.Settings.Services;

/// <summary>
/// Resolves setting values, applying the layering rules.
///
/// Resolution order for a given key: the caller's own value, then the global
/// value, then the default declared in code. Other modules depend on this
/// service rather than reading the table, so the layering can never be
/// implemented twice and differently.
/// </summary>
public sealed class SettingsReader
{
    private readonly SettingsDbContext _dbContext;

    /// <summary>
    /// Initializes the reader.
    /// </summary>
    /// <param name="dbContext">Settings persistence.</param>
    public SettingsReader(SettingsDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    /// <summary>
    /// Resolves the effective raw value of a setting.
    /// </summary>
    /// <param name="key">Setting key.</param>
    /// <param name="userId">Caller, or null to resolve the global value only.</param>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    /// <returns>The effective value, falling back to the declared default.</returns>
    /// <exception cref="ArgumentException">Thrown when the key is not declared.</exception>
    public async Task<string> GetRawAsync(
        string key,
        Guid? userId,
        CancellationToken cancellationToken = default
    )
    {
        var definition =
            SettingKeys.Find(key)
            ?? throw new ArgumentException($"Setting '{key}' is not declared.", nameof(key));

        var candidates = await _dbContext
            .Settings.AsNoTracking()
            .Where(s => s.Key == key && (s.UserId == null || s.UserId == userId))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var userValue =
            userId is null ? null : candidates.FirstOrDefault(s => s.UserId == userId)?.Value;
        var globalValue = candidates.FirstOrDefault(s => s.UserId == null)?.Value;

        return userValue ?? globalValue ?? definition.DefaultValue;
    }

    /// <summary>
    /// Resolves a setting as an integer.
    /// </summary>
    /// <param name="key">Setting key.</param>
    /// <param name="userId">Caller, or null for the global value.</param>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    /// <returns>The parsed value, or the parsed default when the stored value is corrupt.</returns>
    public async Task<int> GetIntAsync(
        string key,
        Guid? userId,
        CancellationToken cancellationToken = default
    )
    {
        var raw = await GetRawAsync(key, userId, cancellationToken).ConfigureAwait(false);
        return int.TryParse(raw, CultureInfo.InvariantCulture, out var value)
            ? value
            : int.Parse(SettingKeys.Find(key)!.DefaultValue, CultureInfo.InvariantCulture);
    }

    /// <summary>
    /// Resolves a setting as a boolean.
    /// </summary>
    /// <param name="key">Setting key.</param>
    /// <param name="userId">Caller, or null for the global value.</param>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    /// <returns>The parsed value, or the parsed default when the stored value is corrupt.</returns>
    public async Task<bool> GetBoolAsync(
        string key,
        Guid? userId,
        CancellationToken cancellationToken = default
    )
    {
        var raw = await GetRawAsync(key, userId, cancellationToken).ConfigureAwait(false);
        return bool.TryParse(raw, out var value)
            ? value
            : bool.Parse(SettingKeys.Find(key)!.DefaultValue);
    }

    /// <summary>
    /// Validates a raw value against a setting's declared type.
    /// </summary>
    /// <param name="definition">The setting declaration.</param>
    /// <param name="value">Raw value to validate.</param>
    /// <returns><c>true</c> when the value parses as the declared type.</returns>
    public static bool IsValid(SettingDefinition definition, string value) =>
        definition.ValueType switch
        {
            SettingValueType.Number => int.TryParse(value, CultureInfo.InvariantCulture, out _),
            SettingValueType.Flag => bool.TryParse(value, out _),
            _ => true,
        };
}
