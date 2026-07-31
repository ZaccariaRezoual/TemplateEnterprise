using EnterpriseFramework.Application.Abstractions;

namespace EnterpriseFramework.Modules.Notifications.Contracts.Events;

/// <summary>
/// Published after a notification has been persisted.
///
/// Implements <see cref="IRealtimeEvent"/>, which is the ONLY thing this
/// module does to become live: it declares that the event matters to one
/// user's clients and on which channel. It has no idea SignalR exists, and
/// the Realtime module has no idea Notifications exists.
///
/// It is a separate event from <c>NotificationRequested</c> on purpose:
/// "someone asked for a notification" and "a notification now exists with
/// this id" are different facts, and only the second can be pushed to a
/// client that may then mark it read.
/// </summary>
/// <param name="Id">Identifier of the stored notification.</param>
/// <param name="UserId">Account the notification belongs to.</param>
/// <param name="Title">Short headline.</param>
/// <param name="Body">Explanatory text.</param>
/// <param name="Level">Severity used for styling.</param>
/// <param name="Link">Optional in-app link.</param>
/// <param name="CreatedAtUtc">When it was created.</param>
public sealed record NotificationCreated(
    Guid Id,
    Guid UserId,
    string Title,
    string Body,
    NotificationLevel Level,
    string? Link,
    DateTime CreatedAtUtc
) : IRealtimeEvent
{
    /// <inheritdoc />
    public DateTime OccurredOnUtc { get; } = DateTime.UtcNow;

    /// <summary>
    /// Only the owner's connections receive it. A notification is personal:
    /// broadcasting it would leak one user's business to everyone online.
    /// </summary>
    public RealtimeAudience Audience => RealtimeAudience.ForUser(UserId);

    /// <inheritdoc />
    public string Channel => "notification.created";
}
