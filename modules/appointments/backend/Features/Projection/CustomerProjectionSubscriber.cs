using EnterpriseFramework.Application.Abstractions;
using EnterpriseFramework.Modules.Appointments.Domain;
using EnterpriseFramework.Modules.Appointments.Persistence;
using EnterpriseFramework.Modules.Auth.Contracts.Events;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace EnterpriseFramework.Modules.Appointments.Features.Projection;

/// <summary>
/// Keeps this module's copy of the people who book in sync with Auth.
///
/// It exists because the public events of this module carry the customer's
/// email and name: a subscriber that has to write to someone must not have to
/// query another module to find out where. Without the projection, either the
/// events would carry only an id — pushing the lookup onto every subscriber —
/// or this module would read Auth's tables, which is the coupling the whole
/// architecture exists to avoid.
///
/// Idempotent: a redelivery must not violate the primary key.
/// </summary>
public sealed class ProjectCustomerOnUserRegistered
    : INotificationHandler<DomainEventNotification<UserRegistered>>
{
    private readonly AppointmentsDbContext _dbContext;

    /// <summary>
    /// Initializes the subscriber.
    /// </summary>
    /// <param name="dbContext">Appointments persistence.</param>
    public ProjectCustomerOnUserRegistered(AppointmentsDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    /// <summary>
    /// Creates the customer row for a newly registered account.
    /// </summary>
    /// <param name="notification">The wrapped registration event.</param>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    public async Task Handle(
        DomainEventNotification<UserRegistered> notification,
        CancellationToken cancellationToken
    )
    {
        var @event = notification.DomainEvent;

        var exists = await _dbContext
            .Customers.AnyAsync(customer => customer.Id == @event.UserId, cancellationToken)
            .ConfigureAwait(false);

        if (exists)
        {
            return;
        }

        _dbContext.Customers.Add(
            CustomerProjection.FromRegistration(@event.UserId, @event.Email, @event.DisplayName)
        );

        await _dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }
}
