using EnterpriseFramework.Application.Abstractions;
using EnterpriseFramework.Modules.Audit.Domain;
using EnterpriseFramework.Modules.Audit.Persistence;
using EnterpriseFramework.Modules.Auth.Contracts.Events;
using MediatR;

namespace EnterpriseFramework.Modules.Audit.Features.RecordLogin;

/// <summary>
/// Records successful logins in the audit trail.
///
/// Logins arrive as EVENTS rather than through the command behavior because
/// what matters is the security fact ("this account signed in"), not the
/// command that produced it. Auth publishes and moves on; it has no idea an
/// audit module exists, and disabling Audit changes nothing about logging in.
/// </summary>
public sealed class RecordLoginOnUserLoggedIn
    : INotificationHandler<DomainEventNotification<UserLoggedIn>>
{
    private readonly AuditDbContext _dbContext;

    /// <summary>
    /// Initializes the subscriber.
    /// </summary>
    /// <param name="dbContext">Audit persistence.</param>
    public RecordLoginOnUserLoggedIn(AuditDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    /// <summary>
    /// Appends the login entry.
    /// </summary>
    /// <param name="notification">The wrapped login event.</param>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    public async Task Handle(
        DomainEventNotification<UserLoggedIn> notification,
        CancellationToken cancellationToken
    )
    {
        _dbContext.Entries.Add(
            AuditEntry.Record(
                notification.DomainEvent.UserId,
                nameof(UserLoggedIn),
                AuditSource.Event,
                succeeded: true,
                correlationId: null
            )
        );
        await _dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }
}
