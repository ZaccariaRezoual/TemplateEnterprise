using System.Text.Json;
using EnterpriseFramework.Application.Abstractions;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;

namespace EnterpriseFramework.Infrastructure.Caching;

/// <summary>
/// <see cref="ICacheService"/> over <see cref="IDistributedCache"/>, which is
/// Redis when configured and an in-memory implementation otherwise.
///
/// Cache failures are SWALLOWED: a cache exists to make a working system
/// faster, so an unreachable Redis must degrade to recomputing rather than
/// turn every request into an error. The one thing that is not tolerated is a
/// stale write — a failed <c>Remove</c> is logged loudly, because silently
/// serving invalidated data is worse than being slow.
/// </summary>
public sealed partial class DistributedCacheService : ICacheService
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);

    private readonly IDistributedCache _cache;
    private readonly ILogger<DistributedCacheService> _logger;

    [LoggerMessage(Level = LogLevel.Warning, Message = "Cache read failed for {Key}; recomputing")]
    private static partial void LogReadFailure(ILogger logger, string key, Exception exception);

    [LoggerMessage(Level = LogLevel.Error, Message = "Cache invalidation failed for {Key}")]
    private static partial void LogInvalidationFailure(
        ILogger logger,
        string key,
        Exception exception
    );

    /// <summary>
    /// Initializes the service.
    /// </summary>
    /// <param name="cache">Underlying distributed cache.</param>
    /// <param name="logger">Reports cache failures.</param>
    public DistributedCacheService(IDistributedCache cache, ILogger<DistributedCacheService> logger)
    {
        _cache = cache;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<TValue> GetOrCreateAsync<TValue>(
        string key,
        Func<CancellationToken, Task<TValue>> factory,
        TimeSpan lifetime,
        CancellationToken cancellationToken = default
    )
    {
        try
        {
            var cached = await _cache.GetStringAsync(key, cancellationToken).ConfigureAwait(false);
            if (cached is not null)
            {
                var value = JsonSerializer.Deserialize<TValue>(cached, SerializerOptions);
                if (value is not null)
                {
                    return value;
                }
            }
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            LogReadFailure(_logger, key, exception);
        }

        var computed = await factory(cancellationToken).ConfigureAwait(false);

        try
        {
            await _cache
                .SetStringAsync(
                    key,
                    JsonSerializer.Serialize(computed, SerializerOptions),
                    new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = lifetime },
                    cancellationToken
                )
                .ConfigureAwait(false);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            // Failing to STORE is harmless: the caller already has its value.
            LogReadFailure(_logger, key, exception);
        }

        return computed;
    }

    /// <inheritdoc />
    public async Task RemoveAsync(string key, CancellationToken cancellationToken = default)
    {
        try
        {
            await _cache.RemoveAsync(key, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            LogInvalidationFailure(_logger, key, exception);
        }
    }
}
