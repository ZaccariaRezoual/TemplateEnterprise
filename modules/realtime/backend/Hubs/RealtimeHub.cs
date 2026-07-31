using EnterpriseFramework.Application.Abstractions;
using EnterpriseFramework.Modules.Realtime.Abstractions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;

namespace EnterpriseFramework.Modules.Realtime.Hubs;

/// <summary>
/// The application's single SignalR hub.
///
/// DELIBERATELY THIN: it only registers and deregisters connections and joins
/// the groups the server decides. It holds no business logic — that lives in
/// <see cref="Services.RealtimeDispatcher"/> and in the modules publishing
/// events — and it exposes NO method a client can call to join a group,
/// because a client that can name a group can read other users' traffic.
///
/// One hub rather than one per feature: each hub costs a separate connection
/// and a separate auth path, while a channel name already separates content.
/// Clients subscribe to channels, not to hubs.
/// </summary>
[Authorize]
public sealed partial class RealtimeHub : Hub
{
    private readonly IConnectionRegistry _connections;
    private readonly ICurrentUser _currentUser;
    private readonly ILogger<RealtimeHub> _logger;

    [LoggerMessage(Level = LogLevel.Debug, Message = "Realtime connection {ConnectionId} opened for {UserId}")]
    private static partial void LogConnected(ILogger logger, string connectionId, Guid userId);

    /// <summary>
    /// Initializes the hub.
    /// </summary>
    /// <param name="connections">Registry tracking live connections.</param>
    /// <param name="currentUser">Identity resolved from the connection's JWT.</param>
    /// <param name="logger">Diagnostic logging.</param>
    public RealtimeHub(
        IConnectionRegistry connections,
        ICurrentUser currentUser,
        ILogger<RealtimeHub> logger
    )
    {
        _connections = connections;
        _currentUser = currentUser;
        _logger = logger;
    }

    /// <summary>
    /// Registers the connection and joins its server-assigned groups.
    /// </summary>
    /// <returns>A task completing when registration finishes.</returns>
    public override async Task OnConnectedAsync()
    {
        var userId = _currentUser.UserId;
        if (userId is null)
        {
            // [Authorize] should have rejected this; aborting is the safe
            // response to a principal we cannot identify.
            Context.Abort();
            return;
        }

        var groups = new List<string> { RealtimeGroups.ForUser(userId.Value) };
        groups.AddRange(_currentUser.Roles.Select(RealtimeGroups.ForRole));

        foreach (var group in groups)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, group).ConfigureAwait(false);
        }

        await _connections
            .AddAsync(
                new RealtimeConnection(Context.ConnectionId, userId.Value, groups, DateTime.UtcNow)
            )
            .ConfigureAwait(false);

        LogConnected(_logger, Context.ConnectionId, userId.Value);
        await base.OnConnectedAsync().ConfigureAwait(false);
    }

    /// <summary>
    /// Deregisters the connection. SignalR removes it from its groups itself.
    /// </summary>
    /// <param name="exception">Error that closed the connection, if any.</param>
    /// <returns>A task completing when deregistration finishes.</returns>
    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        await _connections.RemoveAsync(Context.ConnectionId).ConfigureAwait(false);
        await base.OnDisconnectedAsync(exception).ConfigureAwait(false);
    }

    /// <summary>
    /// Round-trip used by the client to confirm the connection is alive.
    ///
    /// SignalR already pings at the transport level; this proves the
    /// APPLICATION layer is responding, which is what the client's status
    /// indicator actually claims.
    /// </summary>
    /// <returns>The server's UTC time.</returns>
    [System.Diagnostics.CodeAnalysis.SuppressMessage(
        "Performance",
        "CA1822:Mark members as static",
        Justification = "SignalR only dispatches to instance methods on a hub."
    )]
    public DateTime Ping() => DateTime.UtcNow;
}
