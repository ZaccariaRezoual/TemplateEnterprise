using EnterpriseFramework.Domain.Common;

namespace EnterpriseFramework.Application.Abstractions;

/// <summary>
/// Who should receive a realtime event.
///
/// Audiences are resolved SERVER-SIDE only. A client never asks to receive a
/// group's traffic: it would only have to name a group to read other tenants'
/// or other users' events.
/// </summary>
/// <param name="UserId">Deliver to every connection of this account.</param>
/// <param name="Group">Deliver to a server-managed group (e.g. a role).</param>
/// <param name="IsBroadcast">Deliver to every connected client.</param>
public sealed record RealtimeAudience(
    Guid? UserId = null,
    string? Group = null,
    bool IsBroadcast = false
)
{
    /// <summary>Targets every connection of one account.</summary>
    /// <param name="userId">The account to reach.</param>
    /// <returns>The audience.</returns>
    public static RealtimeAudience ForUser(Guid userId) => new(UserId: userId);

    /// <summary>
    /// Targets a server-managed group.
    /// </summary>
    /// <param name="group">Group name, assigned by the server at connection time.</param>
    /// <returns>The audience.</returns>
    public static RealtimeAudience ForGroup(string group) => new(Group: group);

    /// <summary>Targets every connected client. Use sparingly.</summary>
    public static RealtimeAudience Everyone { get; } = new(IsBroadcast: true);
}

/// <summary>
/// Marks a domain event as interesting to connected clients.
///
/// This is the seam that keeps modules free of SignalR. A module says WHAT
/// happened and WHO cares; it never knows that delivery happens over
/// WebSockets, or that a Realtime module exists at all. The Realtime module
/// picks up every event implementing this interface generically, so adding a
/// live-updating feature is a matter of implementing an interface — no
/// registration, no transport code.
///
/// Payloads must be client-safe: whatever an implementation exposes is
/// serialized and sent, so never mark an event carrying secrets or another
/// user's data.
/// </summary>
public interface IRealtimeEvent : IDomainEvent
{
    /// <summary>Who should receive this event.</summary>
    RealtimeAudience Audience { get; }

    /// <summary>
    /// Name clients subscribe to, e.g. "notification.created". Use
    /// "resource.action" so a client can filter without parsing payloads.
    /// </summary>
    string Channel { get; }
}
