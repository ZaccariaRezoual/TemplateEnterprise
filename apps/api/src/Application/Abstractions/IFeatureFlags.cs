namespace EnterpriseFramework.Application.Abstractions;

/// <summary>
/// Tells whether an optional capability is switched on.
///
/// Flags are read through this abstraction so the SOURCE can change —
/// configuration today, a database or LaunchDarkly tomorrow — without
/// touching a single feature. Evaluation is per caller, so a flag can be
/// rolled out to one tenant or one user before everyone.
///
/// A flag is a temporary switch, not a permission: it decides whether a
/// capability EXISTS in this deployment, while permissions decide whether
/// this caller may use it. Using flags for authorization would put a security
/// decision in a file anyone can edit.
/// </summary>
public interface IFeatureFlags
{
    /// <summary>
    /// Evaluates a flag for the current caller.
    /// </summary>
    /// <param name="name">Flag name, e.g. "beta.newDashboard".</param>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    /// <returns>
    /// Whether the capability is on. An UNKNOWN flag is off: a typo must not
    /// silently enable something, and a flag removed from configuration must
    /// not resurrect the code path it guarded.
    /// </returns>
    Task<bool> IsEnabledAsync(string name, CancellationToken cancellationToken = default);

    /// <summary>
    /// Evaluates every known flag for the current caller, for the client to
    /// fetch in one call at startup.
    /// </summary>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    /// <returns>Flag name → whether it is on.</returns>
    Task<IReadOnlyDictionary<string, bool>> GetAllAsync(
        CancellationToken cancellationToken = default
    );
}
