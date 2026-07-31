using EnterpriseFramework.Application.Abstractions;
using EnterpriseFramework.Application.Exceptions;
using EnterpriseFramework.Modules.Notifications.Contracts.Events;
using EnterpriseFramework.Modules.Notifications.Domain;
using EnterpriseFramework.Modules.Notifications.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace EnterpriseFramework.Modules.Notifications.Features;

/// <summary>
/// A notification as exposed to clients.
/// </summary>
/// <param name="Id">Notification identifier.</param>
/// <param name="Title">Short headline.</param>
/// <param name="Body">Explanatory text.</param>
/// <param name="Level">Severity used for styling.</param>
/// <param name="Link">Optional in-app link.</param>
/// <param name="IsUnread">Whether the user has not read it yet.</param>
/// <param name="CreatedAtUtc">When it was created.</param>
public sealed record NotificationDto(
    Guid Id,
    string Title,
    string Body,
    string Level,
    string? Link,
    bool IsUnread,
    DateTime CreatedAtUtc
);

/// <summary>
/// Creates a notification when another module requests one.
///
/// Persisting first is what lets the Realtime module (Fase 6) simply push an
/// already-stored record: a notification the user was offline for is still
/// waiting in the centre when they return.
/// </summary>
public sealed class CreateNotificationOnRequest
    : INotificationHandler<DomainEventNotification<NotificationRequested>>
{
    private readonly NotificationsDbContext _dbContext;

    /// <summary>
    /// Initializes the subscriber.
    /// </summary>
    /// <param name="dbContext">Notifications persistence.</param>
    public CreateNotificationOnRequest(NotificationsDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    /// <summary>
    /// Stores the requested notification.
    /// </summary>
    /// <param name="notification">The wrapped request.</param>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    public async Task Handle(
        DomainEventNotification<NotificationRequested> notification,
        CancellationToken cancellationToken
    )
    {
        _dbContext.Notifications.Add(Notification.FromRequest(notification.DomainEvent));
        await _dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }
}

/// <summary>
/// Lists the caller's notifications, newest first.
/// </summary>
/// <param name="UnreadOnly">Whether to return only unread notifications.</param>
/// <param name="Take">Maximum entries to return; capped by the handler.</param>
public sealed record ListNotificationsQuery(bool UnreadOnly = false, int Take = 50)
    : IRequest<IReadOnlyList<NotificationDto>>;

/// <summary>
/// Handles <see cref="ListNotificationsQuery"/>, scoped to the caller.
/// </summary>
public sealed class ListNotificationsQueryHandler
    : IRequestHandler<ListNotificationsQuery, IReadOnlyList<NotificationDto>>
{
    private const int MaxTake = 100;

    private readonly NotificationsDbContext _dbContext;
    private readonly ICurrentUser _currentUser;

    /// <summary>
    /// Initializes the handler.
    /// </summary>
    /// <param name="dbContext">Notifications persistence.</param>
    /// <param name="currentUser">Identity of the caller.</param>
    public ListNotificationsQueryHandler(
        NotificationsDbContext dbContext,
        ICurrentUser currentUser
    )
    {
        _dbContext = dbContext;
        _currentUser = currentUser;
    }

    /// <summary>
    /// Returns the caller's notifications.
    /// </summary>
    /// <param name="request">The query.</param>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    /// <returns>The notifications, newest first.</returns>
    /// <exception cref="UnauthorizedException">Thrown when the caller is anonymous.</exception>
    public async Task<IReadOnlyList<NotificationDto>> Handle(
        ListNotificationsQuery request,
        CancellationToken cancellationToken
    )
    {
        var userId =
            _currentUser.UserId ?? throw new UnauthorizedException("Authentication is required.");

        // Always filtered by the AUTHENTICATED caller, never by a client-supplied
        // id: this endpoint would otherwise leak everyone's notifications.
        var query = _dbContext.Notifications.AsNoTracking().Where(n => n.UserId == userId);

        if (request.UnreadOnly)
        {
            query = query.Where(n => n.ReadAtUtc == null);
        }

        var notifications = await query
            .OrderByDescending(n => n.CreatedAtUtc)
            .Take(Math.Clamp(request.Take, 1, MaxTake))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return
        [
            .. notifications.Select(n => new NotificationDto(
                n.Id,
                n.Title,
                n.Body,
                n.Level.ToString(),
                n.Link,
                n.IsUnread,
                n.CreatedAtUtc
            )),
        ];
    }
}

/// <summary>
/// Marks one notification, or all of them, as read.
/// </summary>
/// <param name="NotificationId">The notification to mark, or null for all.</param>
public sealed record MarkNotificationsReadCommand(Guid? NotificationId) : IRequest;

/// <summary>
/// Handles <see cref="MarkNotificationsReadCommand"/>.
/// </summary>
public sealed class MarkNotificationsReadCommandHandler
    : IRequestHandler<MarkNotificationsReadCommand>
{
    private readonly NotificationsDbContext _dbContext;
    private readonly ICurrentUser _currentUser;

    /// <summary>
    /// Initializes the handler.
    /// </summary>
    /// <param name="dbContext">Notifications persistence.</param>
    /// <param name="currentUser">Identity of the caller.</param>
    public MarkNotificationsReadCommandHandler(
        NotificationsDbContext dbContext,
        ICurrentUser currentUser
    )
    {
        _dbContext = dbContext;
        _currentUser = currentUser;
    }

    /// <summary>
    /// Marks the caller's notifications as read.
    /// </summary>
    /// <param name="request">The command.</param>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    /// <exception cref="UnauthorizedException">Thrown when the caller is anonymous.</exception>
    public async Task Handle(
        MarkNotificationsReadCommand request,
        CancellationToken cancellationToken
    )
    {
        var userId =
            _currentUser.UserId ?? throw new UnauthorizedException("Authentication is required.");

        // The user id is part of the filter, not just the lookup: marking
        // someone else's notification must be impossible, not merely unlikely.
        var query = _dbContext.Notifications.Where(n => n.UserId == userId && n.ReadAtUtc == null);

        if (request.NotificationId is { } id)
        {
            query = query.Where(n => n.Id == id);
        }

        foreach (var notification in await query.ToListAsync(cancellationToken).ConfigureAwait(false))
        {
            notification.MarkAsRead();
        }

        await _dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }
}
