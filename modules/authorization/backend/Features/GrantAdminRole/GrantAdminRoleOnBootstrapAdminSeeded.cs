using EnterpriseFramework.Application.Abstractions;
using EnterpriseFramework.Modules.Auth.Contracts.Events;
using EnterpriseFramework.Modules.Authorization.Domain;
using EnterpriseFramework.Modules.Authorization.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace EnterpriseFramework.Modules.Authorization.Features.GrantAdminRole;

/// <summary>
/// Grants the <see cref="BuiltInRoles.Admin"/> role to the bootstrap
/// administrator account announced by Auth at startup.
///
/// This is what solves the chicken-and-egg problem of a fresh installation:
/// registration grants only the default role, and promoting an account
/// requires <c>roles.write</c>, which nobody holds yet. Auth creates the
/// account and knows nothing about roles; this module decides that the
/// bootstrap account is an administrator.
///
/// Disabling this module leaves the account existing but powerless, which is
/// the correct failure mode — permissions are this module's business.
/// </summary>
public sealed partial class GrantAdminRoleOnBootstrapAdminSeeded
    : INotificationHandler<DomainEventNotification<BootstrapAdminSeeded>>
{
    private readonly AuthorizationDbContext _dbContext;
    private readonly ILogger<GrantAdminRoleOnBootstrapAdminSeeded> _logger;

    [LoggerMessage(
        Level = LogLevel.Warning,
        Message = "Role '{RoleName}' is missing; bootstrap admin {UserId} was not promoted"
    )]
    private static partial void LogMissingAdminRole(ILogger logger, string roleName, Guid userId);

    [LoggerMessage(Level = LogLevel.Information, Message = "Bootstrap admin {UserId} granted '{RoleName}'")]
    private static partial void LogGranted(ILogger logger, Guid userId, string roleName);

    /// <summary>
    /// Initializes the subscriber.
    /// </summary>
    /// <param name="dbContext">Authorization persistence.</param>
    /// <param name="logger">Reports the promotion, or a missing role.</param>
    public GrantAdminRoleOnBootstrapAdminSeeded(
        AuthorizationDbContext dbContext,
        ILogger<GrantAdminRoleOnBootstrapAdminSeeded> logger
    )
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    /// <summary>
    /// Promotes the seeded account to administrator.
    /// </summary>
    /// <param name="notification">The wrapped bootstrap event.</param>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    public async Task Handle(
        DomainEventNotification<BootstrapAdminSeeded> notification,
        CancellationToken cancellationToken
    )
    {
        var userId = notification.DomainEvent.UserId;

        var adminRole = await _dbContext
            .Roles.SingleOrDefaultAsync(role => role.Name == BuiltInRoles.Admin, cancellationToken)
            .ConfigureAwait(false);

        if (adminRole is null)
        {
            LogMissingAdminRole(_logger, BuiltInRoles.Admin, userId);
            return;
        }

        var alreadyGranted = await _dbContext
            .UserRoles.AnyAsync(
                grant => grant.UserId == userId && grant.RoleId == adminRole.Id,
                cancellationToken
            )
            .ConfigureAwait(false);

        if (alreadyGranted)
        {
            return;
        }

        _dbContext.UserRoles.Add(UserRole.Grant(userId, adminRole.Id));
        await _dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        LogGranted(_logger, userId, BuiltInRoles.Admin);
    }
}
