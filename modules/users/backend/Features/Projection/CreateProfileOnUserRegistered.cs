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
    private readonly ITenantContext _tenantContext;

    /// <summary>
    /// Initializes the subscriber.
    /// </summary>
    /// <param name="dbContext">Users persistence.</param>
    /// <param name="tenantContext">Tenant the new profile belongs to.</param>
    public CreateProfileOnUserRegistered(
        UsersDbContext dbContext,
        ITenantContext tenantContext
    )
    {
        _dbContext = dbContext;
        _tenantContext = tenantContext;
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

        // IgnoreQueryFilters: the idempotency check must see the row even if
        // it belongs to another tenant, or a replay would violate the primary
        // key instead of returning early.
        var exists = await _dbContext
            .Profiles.IgnoreQueryFilters()
            .AnyAsync(p => p.Id == @event.UserId, cancellationToken)
            .ConfigureAwait(false);
        if (exists)
        {
            return;
        }

        _dbContext.Profiles.Add(
            UserProfile.FromRegistration(
                @event.UserId,
                @event.Email,
                @event.DisplayName,
                _tenantContext.TenantId ?? Guid.Empty
            )
        );
        await _dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }
}
