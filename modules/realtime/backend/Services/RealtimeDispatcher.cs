using EnterpriseFramework.Application.Abstractions;
using EnterpriseFramework.Modules.Realtime.Hubs;
using Microsoft.AspNetCore.SignalR;

namespace EnterpriseFramework.Modules.Realtime.Services;

/// <summary>
/// Client-visible envelope of a realtime event.
///
/// The channel travels alongside the payload so a client can route on one
/// field instead of inspecting shapes, and the timestamp lets a client that
/// reconnects discard anything it already processed.
/// </summary>
/// <param name="Channel">Channel the event was published on.</param>
/// <param name="Payload">The event itself, serialized as-is.</param>
/// <param name="OccurredOnUtc">When the event happened on the server.</param>
public sealed record RealtimeEnvelope(string Channel, object Payload, DateTime OccurredOnUtc);

/// <summary>
/// Turns a realtime event into the right SignalR send.
///
/// This is where the hub's logic lives, per the "thin hubs" rule: the hub
/// manages connections, this service decides who receives what. Keeping it
/// out of the hub also makes it directly unit-testable — a hub instance needs
/// a whole connection context, a dispatcher needs only a hub context.
/// </summary>
public sealed class RealtimeDispatcher
{
    /// <summary>Client-side method every event arrives on.</summary>
    public const string ClientMethod = "realtimeEvent";

    private readonly IHubContext<RealtimeHub> _hub;

    /// <summary>
    /// Initializes the dispatcher.
    /// </summary>
    /// <param name="hub">Context used to reach connected clients.</param>
    public RealtimeDispatcher(IHubContext<RealtimeHub> hub)
    {
        _hub = hub;
    }

    /// <summary>
    /// Delivers an event to its audience.
    /// </summary>
    /// <param name="realtimeEvent">The event to deliver.</param>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    /// <returns>A task completing when the send is handed to the transport.</returns>
    public Task DispatchAsync(
        IRealtimeEvent realtimeEvent,
        CancellationToken cancellationToken = default
    )
    {
        var envelope = new RealtimeEnvelope(
            realtimeEvent.Channel,
            realtimeEvent,
            realtimeEvent.OccurredOnUtc
        );

        var target = ResolveTarget(realtimeEvent.Audience);

        return target.SendAsync(ClientMethod, envelope, cancellationToken);
    }

    /// <summary>
    /// Maps an audience to the SignalR client set that represents it.
    /// </summary>
    /// <param name="audience">The declared audience.</param>
    /// <returns>The clients to send to.</returns>
    private IClientProxy ResolveTarget(RealtimeAudience audience)
    {
        // Order matters: a specific user wins over a group, and broadcast is
        // the last resort. An audience that names nothing reaches nobody
        // rather than everybody — the safe direction to fail.
        if (audience.UserId is { } userId)
        {
            return _hub.Clients.Group(RealtimeGroups.ForUser(userId));
        }

        if (!string.IsNullOrWhiteSpace(audience.Group))
        {
            return _hub.Clients.Group(audience.Group);
        }

        return audience.IsBroadcast ? _hub.Clients.All : new NoClients();
    }

    /// <summary>
    /// Null object used when an audience targets nobody, so callers never
    /// need a null check and a misconfigured event is silently harmless
    /// rather than accidentally broadcast.
    /// </summary>
    private sealed class NoClients : IClientProxy
    {
        public Task SendCoreAsync(
            string method,
            object?[] args,
            CancellationToken cancellationToken = default
        ) => Task.CompletedTask;
    }
}
