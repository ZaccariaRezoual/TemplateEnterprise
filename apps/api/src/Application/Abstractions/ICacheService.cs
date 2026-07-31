namespace EnterpriseFramework.Application.Abstractions;

/// <summary>
/// Read-through cache for expensive results.
///
/// Backed by Redis when configured and by process memory otherwise, so the
/// same code runs in both — but the difference matters: an in-memory cache is
/// per-replica, so an invalidation on one instance leaves the others stale.
/// That is acceptable in development and wrong behind a load balancer.
///
/// Cache what is expensive AND tolerant of being briefly stale. Anything a
/// user must see immediately after their own action does not belong here.
/// </summary>
public interface ICacheService
{
    /// <summary>
    /// Returns a cached value, computing and storing it on a miss.
    /// </summary>
    /// <typeparam name="TValue">Type of the cached value.</typeparam>
    /// <param name="key">
    /// Cache key. Include everything the result depends on — tenant, user,
    /// filters — or one caller's result is served to another.
    /// </param>
    /// <param name="factory">Computes the value when it is not cached.</param>
    /// <param name="lifetime">How long to keep it.</param>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    /// <returns>The cached or freshly computed value.</returns>
    Task<TValue> GetOrCreateAsync<TValue>(
        string key,
        Func<CancellationToken, Task<TValue>> factory,
        TimeSpan lifetime,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    /// Removes a cached entry. Removing an absent key is not an error.
    /// </summary>
    /// <param name="key">Key to drop.</param>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    Task RemoveAsync(string key, CancellationToken cancellationToken = default);
}
