using System.Collections.Concurrent;
using EnterpriseFramework.Modules.Realtime.Abstractions;

namespace EnterpriseFramework.Modules.Realtime.Services;

/// <summary>
/// Connection registry held in process memory.
///
/// Used when Redis is not configured, so the framework runs with one less
/// dependency during development and in tests. It is correct for a SINGLE
/// instance only: with more than one replica each would report its own
/// connections as the whole truth. The module logs a warning at startup so
/// this never becomes an accidental production setup.
/// </summary>
public sealed class InMemoryConnectionRegistry : IConnectionRegistry
{
    private readonly ConcurrentDictionary<string, RealtimeConnection> _connections = new();

    /// <inheritdoc />
    public Task AddAsync(
        RealtimeConnection connection,
        CancellationToken cancellationToken = default
    )
    {
        _connections[connection.ConnectionId] = connection;
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task RemoveAsync(string connectionId, CancellationToken cancellationToken = default)
    {
        _connections.TryRemove(connectionId, out _);
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<Guid>> GetOnlineUsersAsync(
        CancellationToken cancellationToken = default
    ) =>
        Task.FromResult<IReadOnlyList<Guid>>(
            [.. _connections.Values.Select(c => c.UserId).Distinct()]
        );

    /// <inheritdoc />
    public Task<bool> IsOnlineAsync(Guid userId, CancellationToken cancellationToken = default) =>
        Task.FromResult(_connections.Values.Any(c => c.UserId == userId));
}
