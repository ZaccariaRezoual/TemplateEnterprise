namespace EnterpriseFramework.Modules.Realtime.Abstractions;

/// <summary>
/// A live client connection.
/// </summary>
/// <param name="ConnectionId">SignalR connection identifier.</param>
/// <param name="UserId">Authenticated account behind the connection.</param>
/// <param name="Groups">Server-assigned groups this connection belongs to.</param>
/// <param name="ConnectedAtUtc">When the connection was established.</param>
public sealed record RealtimeConnection(
    string ConnectionId,
    Guid UserId,
    IReadOnlyList<string> Groups,
    DateTime ConnectedAtUtc
);

/// <summary>
/// Tracks who is currently connected and to which groups.
///
/// It exists because SignalR itself has no queryable connection list, and
/// because the answer must be correct ACROSS INSTANCES: with two API replicas
/// behind a load balancer, "is Ada online?" cannot be answered from one
/// process's memory. The Redis-backed implementation makes presence a
/// property of the deployment, not of a single instance.
/// </summary>
public interface IConnectionRegistry
{
    /// <summary>
    /// Records a new connection.
    /// </summary>
    /// <param name="connection">The connection to track.</param>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    Task AddAsync(RealtimeConnection connection, CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes a connection.
    /// </summary>
    /// <param name="connectionId">Connection that dropped.</param>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    Task RemoveAsync(string connectionId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Lists the accounts with at least one live connection.
    /// </summary>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    /// <returns>Distinct account identifiers currently online.</returns>
    Task<IReadOnlyList<Guid>> GetOnlineUsersAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Tells whether an account has any live connection.
    /// </summary>
    /// <param name="userId">Account to check.</param>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    /// <returns><c>true</c> when at least one connection is open.</returns>
    Task<bool> IsOnlineAsync(Guid userId, CancellationToken cancellationToken = default);
}
