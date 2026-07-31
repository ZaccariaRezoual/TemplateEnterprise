using EnterpriseFramework.Domain.Common;

namespace EnterpriseFramework.Modules.Notifications.Contracts.Events;

/// <summary>Severity of a notification, used to style it.</summary>
public enum NotificationLevel
{
    /// <summary>Neutral information.</summary>
    Info = 0,

    /// <summary>Something completed successfully.</summary>
    Success = 1,

    /// <summary>Something needs attention.</summary>
    Warning = 2,

    /// <summary>Something failed.</summary>
    Error = 3,
}

/// <summary>
/// Asks that a user be notified.
///
/// PUBLIC CONTRACT: any module publishes this on the event bus to reach a
/// user, without referencing the Notifications module. That keeps notifying
/// a fire-and-forget concern — the publisher does not care whether delivery
/// is a bell in the UI today and a push message tomorrow.
/// </summary>
/// <param name="UserId">Account to notify.</param>
/// <param name="Title">Short headline.</param>
/// <param name="Body">Explanatory text.</param>
/// <param name="Level">Severity used for styling.</param>
/// <param name="Link">Optional in-app link the notification points at.</param>
public sealed record NotificationRequested(
    Guid UserId,
    string Title,
    string Body,
    NotificationLevel Level = NotificationLevel.Info,
    string? Link = null
) : IDomainEvent
{
    /// <inheritdoc />
    public DateTime OccurredOnUtc { get; } = DateTime.UtcNow;
}
