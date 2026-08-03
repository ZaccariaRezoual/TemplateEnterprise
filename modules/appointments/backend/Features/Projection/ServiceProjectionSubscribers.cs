using EnterpriseFramework.Application.Abstractions;
using EnterpriseFramework.Modules.Appointments.Domain;
using EnterpriseFramework.Modules.Appointments.Persistence;
using EnterpriseFramework.Modules.Services.Contracts.Events;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace EnterpriseFramework.Modules.Appointments.Features.Projection;

/// <summary>
/// Keeps this module's copy of the bookable services in sync with the
/// Services module.
///
/// The event-driven projection pattern the framework is built on: Services
/// owns the catalogue and announces changes, Appointments maintains its own
/// copy of exactly the four fields it needs — title, slug, duration and
/// whether it takes bookings. No module ever queries another module's tables,
/// so either can be removed without leaving a dangling join.
///
/// Every handler here is idempotent: a redelivery, or a rebuild of the
/// projection, must not create duplicates or lose state.
/// </summary>
public sealed class ProjectServiceOnPublished
    : INotificationHandler<DomainEventNotification<ServicePublished>>
{
    private readonly AppointmentsDbContext _dbContext;

    /// <summary>
    /// Initializes the subscriber.
    /// </summary>
    /// <param name="dbContext">Appointments persistence.</param>
    public ProjectServiceOnPublished(AppointmentsDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    /// <summary>
    /// Creates or refreshes the projection of a newly published service.
    /// </summary>
    /// <param name="notification">The wrapped publication event.</param>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    public async Task Handle(
        DomainEventNotification<ServicePublished> notification,
        CancellationToken cancellationToken
    )
    {
        var @event = notification.DomainEvent;

        var existing = await _dbContext
            .Services.SingleOrDefaultAsync(
                service => service.Id == @event.ServiceId,
                cancellationToken
            )
            .ConfigureAwait(false);

        if (existing is null)
        {
            _dbContext.Services.Add(
                ServiceProjection.FromPublication(
                    @event.ServiceId,
                    @event.Title,
                    @event.Slug,
                    @event.DurationMinutes,
                    @event.IsBookable
                )
            );
        }
        else
        {
            existing.Apply(@event.Title, @event.Slug, @event.DurationMinutes, @event.IsBookable);
        }

        await _dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }
}

/// <summary>
/// Refreshes the projection when a published service changes.
///
/// It handles the withdrawal too: the Services module raises this event —
/// with <c>IsBookable</c> already false — rather than a separate
/// "unpublished" one, so there is no case to forget.
/// </summary>
public sealed class ProjectServiceOnUpdated
    : INotificationHandler<DomainEventNotification<ServiceUpdated>>
{
    private readonly AppointmentsDbContext _dbContext;

    /// <summary>
    /// Initializes the subscriber.
    /// </summary>
    /// <param name="dbContext">Appointments persistence.</param>
    public ProjectServiceOnUpdated(AppointmentsDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    /// <summary>
    /// Applies the announced change.
    /// </summary>
    /// <param name="notification">The wrapped update event.</param>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    public async Task Handle(
        DomainEventNotification<ServiceUpdated> notification,
        CancellationToken cancellationToken
    )
    {
        var @event = notification.DomainEvent;

        var existing = await _dbContext
            .Services.SingleOrDefaultAsync(
                service => service.Id == @event.ServiceId,
                cancellationToken
            )
            .ConfigureAwait(false);

        if (existing is null)
        {
            // The projection can legitimately be missing: this module may
            // have been installed after the service was published. Building
            // the row from the update is how it catches up, instead of
            // silently ignoring a service nobody can then book.
            _dbContext.Services.Add(
                ServiceProjection.FromPublication(
                    @event.ServiceId,
                    @event.Title,
                    @event.Slug,
                    @event.DurationMinutes,
                    @event.IsBookable
                )
            );
        }
        else
        {
            existing.Apply(@event.Title, @event.Slug, @event.DurationMinutes, @event.IsBookable);
        }

        await _dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }
}

/// <summary>
/// Stops new bookings when a service is withdrawn from the catalogue.
///
/// Appointments ALREADY taken are left alone, deliberately: people have them
/// in their calendars, and the business still means to honour them. This is
/// the same reason the Services module archives instead of deleting.
/// </summary>
public sealed class ProjectServiceOnArchived
    : INotificationHandler<DomainEventNotification<ServiceArchived>>
{
    private readonly AppointmentsDbContext _dbContext;

    /// <summary>
    /// Initializes the subscriber.
    /// </summary>
    /// <param name="dbContext">Appointments persistence.</param>
    public ProjectServiceOnArchived(AppointmentsDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    /// <summary>
    /// Marks the projection as archived.
    /// </summary>
    /// <param name="notification">The wrapped archival event.</param>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    public async Task Handle(
        DomainEventNotification<ServiceArchived> notification,
        CancellationToken cancellationToken
    )
    {
        var existing = await _dbContext
            .Services.SingleOrDefaultAsync(
                service => service.Id == notification.DomainEvent.ServiceId,
                cancellationToken
            )
            .ConfigureAwait(false);

        if (existing is null)
        {
            // Nothing to archive: a service this module never knew about,
            // which is not an error — it simply was never bookable here.
            return;
        }

        existing.Archive();
        await _dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }
}
