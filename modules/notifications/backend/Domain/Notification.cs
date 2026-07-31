using EnterpriseFramework.Domain.Common;
using EnterpriseFramework.Modules.Notifications.Contracts.Events;

namespace EnterpriseFramework.Modules.Notifications.Domain;

/// <summary>
/// A notification addressed to one user.
/// </summary>
public sealed class Notification : EntityBase<Guid>
{
    private Notification() { }

    /// <summary>Account the notification belongs to.</summary>
    public Guid UserId { get; private set; }

    /// <summary>Short headline.</summary>
    public string Title { get; private set; } = string.Empty;

    /// <summary>Explanatory text.</summary>
    public string Body { get; private set; } = string.Empty;

    /// <summary>Severity used for styling.</summary>
    public NotificationLevel Level { get; private set; }

    /// <summary>Optional in-app link.</summary>
    public string? Link { get; private set; }

    /// <summary>UTC instant the user read it; null while unread.</summary>
    public DateTime? ReadAtUtc { get; private set; }

    /// <summary>UTC instant it was created.</summary>
    public DateTime CreatedAtUtc { get; private set; }

    /// <summary>Whether the user has not read it yet.</summary>
    public bool IsUnread => ReadAtUtc is null;

    /// <summary>
    /// Creates a notification from a request published by another module.
    /// </summary>
    /// <param name="request">The published request.</param>
    /// <returns>The notification to persist.</returns>
    public static Notification FromRequest(NotificationRequested request) =>
        new()
        {
            Id = Guid.NewGuid(),
            UserId = request.UserId,
            Title = request.Title,
            Body = request.Body,
            Level = request.Level,
            Link = request.Link,
            CreatedAtUtc = DateTime.UtcNow,
        };

    /// <summary>
    /// Marks the notification as read. Idempotent: the first read wins, so a
    /// double-click does not rewrite history.
    /// </summary>
    public void MarkAsRead() => ReadAtUtc ??= DateTime.UtcNow;
}
