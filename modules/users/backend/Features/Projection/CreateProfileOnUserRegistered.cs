using EnterpriseFramework.Application.Abstractions;
using EnterpriseFramework.Modules.Auth.Contracts.Events;
using EnterpriseFramework.Modules.Users.Domain;
using EnterpriseFramework.Modules.Users.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace EnterpriseFramework.Modules.Users.Features.Projection;

/// <summary>
/// Keeps this module's account projection in sync with Auth.
///
/// This is the event-driven projection pattern the framework is built on:
/// Auth owns accounts and announces changes; Users maintains its own copy of
/// exactly the fields it needs. No module ever queries another module's
/// tables, so a module can be removed without leaving dangling joins.
///
/// Idempotent: replaying the event (redelivery, or a rebuild of the
/// projection) must not create duplicates.
/// </summary>
public sealed class CreateProfileOnUserRegistered
    : INotificationHandler<DomainEventNotification<UserRegistered>>
{
    private readonly UsersDbContext _dbContext;

    /// <summary>
    /// Initializes the subscriber.
    /// </summary>
    /// <param name="dbContext">Users persistence.</param>
    public CreateProfileOnUserRegistered(UsersDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    /// <summary>
    /// Creates the profile row for a newly registered account.
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
            .Profiles.AnyAsync(p => p.Id == @event.UserId, cancellationToken)
            .ConfigureAwait(false);
        if (exists)
        {
            return;
        }

        _dbContext.Profiles.Add(
            UserProfile.FromRegistration(@event.UserId, @event.Email, @event.DisplayName)
        );
        await _dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }
}
