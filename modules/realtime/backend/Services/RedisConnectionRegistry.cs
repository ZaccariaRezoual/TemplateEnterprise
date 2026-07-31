using System.Globalization;
using EnterpriseFramework.Modules.Realtime.Abstractions;
using StackExchange.Redis;

namespace EnterpriseFramework.Modules.Realtime.Services;

/// <summary>
/// Connection registry backed by Redis, so presence is correct across API
/// instances.
///
/// Layout: one hash per connection (`realtime:conn:{id}`) plus a set per
/// account (`realtime:user:{userId}`) holding that account's connection ids.
/// The set answers "is this user online?" in one round-trip, which matters
/// because presence is queried far more often than it changes.
///
/// Entries carry a TTL: a process killed mid-flight never runs its disconnect
/// handler, and without expiry those connections would look online forever.
/// </summary>
public sealed class RedisConnectionRegistry : IConnectionRegistry
{
    private const string ConnectionKeyPrefix = "realtime:conn:";
    private const string UserKeyPrefix = "realtime:user:";
    private const string OnlineUsersKey = "realtime:online";

    // Comfortably longer than any reconnect window, short enough that a
    // crashed instance's ghosts disappear within minutes.
    private static readonly TimeSpan EntryLifetime = TimeSpan.FromHours(12);

    private readonly IConnectionMultiplexer _redis;

    /// <summary>
    /// Initializes the registry.
    /// </summary>
    /// <param name="redis">Shared Redis connection.</param>
    public RedisConnectionRegistry(IConnectionMultiplexer redis)
    {
        _redis = redis;
    }

    /// <inheritdoc />
    public async Task AddAsync(
        RealtimeConnection connection,
        CancellationToken cancellationToken = default
    )
    {
        var database = _redis.GetDatabase();
        var connectionKey = ConnectionKeyPrefix + connection.ConnectionId;
        var userKey = UserKeyPrefix + connection.UserId.ToString("N");

        await database
            .HashSetAsync(
                connectionKey,
                [
                    new HashEntry("userId", connection.UserId.ToString("N")),
                    new HashEntry("groups", string.Join(',', connection.Groups)),
                    new HashEntry(
                        "connectedAt",
                        connection.ConnectedAtUtc.ToString("O", CultureInfo.InvariantCulture)
                    ),
                ]
            )
            .ConfigureAwait(false);
        await database.KeyExpireAsync(connectionKey, EntryLifetime).ConfigureAwait(false);

        await database
            .SetAddAsync(userKey, connection.ConnectionId)
            .ConfigureAwait(false);
        await database.KeyExpireAsync(userKey, EntryLifetime).ConfigureAwait(false);

        await database
            .SetAddAsync(OnlineUsersKey, connection.UserId.ToString("N"))
            .ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task RemoveAsync(
        string connectionId,
        CancellationToken cancellationToken = default
    )
    {
        var database = _redis.GetDatabase();
        var connectionKey = ConnectionKeyPrefix + connectionId;

        var storedUserId = await database
            .HashGetAsync(connectionKey, "userId")
            .ConfigureAwait(false);
        await database.KeyDeleteAsync(connectionKey).ConfigureAwait(false);

        if (storedUserId.IsNullOrEmpty)
        {
            return;
        }

        var userKey = UserKeyPrefix + storedUserId!;
        await database.SetRemoveAsync(userKey, connectionId).ConfigureAwait(false);

        // The account goes offline only when its LAST connection closes:
        // a user with three tabs open is still online after closing one.
        if (await database.SetLengthAsync(userKey).ConfigureAwait(false) == 0)
        {
            await database.SetRemoveAsync(OnlineUsersKey, storedUserId!).ConfigureAwait(false);
        }
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<Guid>> GetOnlineUsersAsync(
        CancellationToken cancellationToken = default
    )
    {
        var members = await _redis
            .GetDatabase()
            .SetMembersAsync(OnlineUsersKey)
            .ConfigureAwait(false);

        return
        [
            .. members
                .Select(member => Guid.TryParse(member.ToString(), out var id) ? id : (Guid?)null)
                .Where(id => id is not null)
                .Select(id => id!.Value),
        ];
    }

    /// <inheritdoc />
    public Task<bool> IsOnlineAsync(Guid userId, CancellationToken cancellationToken = default) =>
        _redis.GetDatabase().SetContainsAsync(OnlineUsersKey, userId.ToString("N"));
}
