using EnterpriseFramework.Application.Abstractions;
using EnterpriseFramework.Modules.Auth.Contracts.Events;
using EnterpriseFramework.Modules.Authorization.Domain;
using EnterpriseFramework.Modules.Authorization.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace EnterpriseFramework.Modules.Authorization.Features.GrantDefaultRole;

/// <summary>
/// Grants the default "BasicUser" role to every newly registered account.
///
/// This is the module boundary working as designed: Auth publishes that an
/// account exists and knows nothing about roles; Authorization decides what a
/// new account may do. It subscribes to Auth's public CONTRACT assembly, not
/// to the Auth implementation — and disabling this module leaves registration
/// working, with the account simply holding no permissions.
/// </summary>
public sealed partial class GrantDefaultRoleOnUserRegistered
    : INotificationHandler<DomainEventNotification<UserRegistered>>
{
    private readonly AuthorizationDbContext _dbContext;
    private readonly ILogger<GrantDefaultRoleOnUserRegistered> _logger;

    [LoggerMessage(
        Level = LogLevel.Warning,
        Message = "Default role '{RoleName}' is missing; user {UserId} was granted no role"
    )]
    private static partial void LogMissingDefaultRole(ILogger logger, string roleName, Guid userId);

    /// <summary>
    /// Initializes the subscriber.
    /// </summary>
    /// <param name="dbContext">Authorization persistence.</param>
    /// <param name="logger">Reports a missing default role.</param>
    public GrantDefaultRoleOnUserRegistered(
        AuthorizationDbContext dbContext,
        ILogger<GrantDefaultRoleOnUserRegistered> logger
    )
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    /// <summary>
    /// Grants the default role to the new account.
    /// </summary>
    /// <param name="notification">The wrapped registration event.</param>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    public async Task Handle(
        DomainEventNotification<UserRegistered> notification,
        CancellationToken cancellationToken
    )
    {
        var userId = notification.DomainEvent.UserId;

        var defaultRole = await _dbContext
            .Roles.SingleOrDefaultAsync(r => r.Name == BuiltInRoles.BasicUser, cancellationToken)
            .ConfigureAwait(false);

        if (defaultRole is null)
        {
            LogMissingDefaultRole(_logger, BuiltInRoles.BasicUser, userId);
            return;
        }

        var alreadyGranted = await _dbContext
            .UserRoles.AnyAsync(
                ur => ur.UserId == userId && ur.RoleId == defaultRole.Id,
                cancellationToken
            )
            .ConfigureAwait(false);

        if (!alreadyGranted)
        {
            _dbContext.UserRoles.Add(UserRole.Grant(userId, defaultRole.Id));
            await _dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }
    }
}
