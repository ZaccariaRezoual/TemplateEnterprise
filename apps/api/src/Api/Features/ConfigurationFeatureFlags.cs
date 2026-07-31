using EnterpriseFramework.Application.Abstractions;
using Microsoft.Extensions.Options;

namespace EnterpriseFramework.Api.Features;

/// <summary>
/// Default <see cref="IFeatureFlags"/>, reading from configuration.
///
/// Evaluation order per flag: an explicit per-user allow, then a per-tenant
/// allow, then the flag's default. Allow-lists only — there is no way to
/// switch a flag OFF for one user, because "on for everyone except…" turns a
/// rollout switch into an authorization rule, and authorization belongs to
/// permissions.
///
/// Registered as SCOPED because evaluation depends on the caller.
/// </summary>
public sealed class ConfigurationFeatureFlags : IFeatureFlags
{
    private readonly IOptionsMonitor<FeatureFlagOptions> _options;
    private readonly ICurrentUser _currentUser;
    private readonly ITenantContext _tenantContext;

    /// <summary>
    /// Initializes the provider.
    /// </summary>
    /// <param name="options">
    /// Monitored, not snapshotted: configuration reload then changes flags
    /// without a restart, which is the whole point of a switch.
    /// </param>
    /// <param name="currentUser">Caller identity, for per-user rollout.</param>
    /// <param name="tenantContext">Caller tenant, for per-tenant rollout.</param>
    public ConfigurationFeatureFlags(
        IOptionsMonitor<FeatureFlagOptions> options,
        ICurrentUser currentUser,
        ITenantContext tenantContext
    )
    {
        _options = options;
        _currentUser = currentUser;
        _tenantContext = tenantContext;
    }

    /// <inheritdoc />
    public Task<bool> IsEnabledAsync(string name, CancellationToken cancellationToken = default) =>
        Task.FromResult(
            _options.CurrentValue.Flags.TryGetValue(name, out var definition)
            && Evaluate(definition)
        );

    /// <inheritdoc />
    public Task<IReadOnlyDictionary<string, bool>> GetAllAsync(
        CancellationToken cancellationToken = default
    ) =>
        Task.FromResult<IReadOnlyDictionary<string, bool>>(
            _options.CurrentValue.Flags.ToDictionary(
                flag => flag.Key,
                flag => Evaluate(flag.Value),
                StringComparer.OrdinalIgnoreCase
            )
        );

    private bool Evaluate(FeatureFlagDefinition definition)
    {
        if (_currentUser.UserId is { } userId && definition.EnabledForUsers.Contains(userId))
        {
            return true;
        }

        if (_tenantContext.TenantId is { } tenantId && definition.EnabledForTenants.Contains(tenantId))
        {
            return true;
        }

        return definition.Enabled;
    }
}
