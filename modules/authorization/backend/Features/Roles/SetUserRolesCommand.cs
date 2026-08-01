using EnterpriseFramework.Application.Abstractions;
using EnterpriseFramework.Application.Exceptions;
using EnterpriseFramework.Modules.Authorization.Domain;
using EnterpriseFramework.Modules.Authorization.Persistence;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace EnterpriseFramework.Modules.Authorization.Features.Roles;

/// <summary>
/// Replaces the roles assigned to an account.
/// </summary>
/// <param name="UserId">Account whose roles are being set.</param>
/// <param name="RoleNames">The complete new set of role names.</param>
public sealed record SetUserRolesCommand(Guid UserId, IReadOnlyList<string> RoleNames) : IRequest;

/// <summary>Validation rules for <see cref="SetUserRolesCommand"/>.</summary>
public sealed class SetUserRolesCommandValidator : AbstractValidator<SetUserRolesCommand>
{
    /// <summary>Initializes the rules.</summary>
    public SetUserRolesCommandValidator()
    {
        RuleFor(c => c.UserId).NotEmpty();
        RuleFor(c => c.RoleNames).NotNull();
    }
}

/// <summary>
/// Handles <see cref="SetUserRolesCommand"/>.
///
/// Safety rule: an administrator cannot remove their own last administrator
/// role. Without it, one careless save can leave a system with no one able to
/// administer it — an unrecoverable state.
/// </summary>
public sealed class SetUserRolesCommandHandler : IRequestHandler<SetUserRolesCommand>
{
    private readonly AuthorizationDbContext _dbContext;
    private readonly ICurrentUser _currentUser;

    /// <summary>
    /// Initializes the handler.
    /// </summary>
    /// <param name="dbContext">Authorization persistence.</param>
    /// <param name="currentUser">Identity of the caller, for the self-demotion guard.</param>
    public SetUserRolesCommandHandler(AuthorizationDbContext dbContext, ICurrentUser currentUser)
    {
        _dbContext = dbContext;
        _currentUser = currentUser;
    }

    /// <summary>
    /// Applies the new role set.
    /// </summary>
    /// <param name="request">The validated command.</param>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    /// <exception cref="NotFoundException">Thrown when a role name does not exist.</exception>
    /// <exception cref="BusinessException">
    /// Thrown when callers would strip their own administrator role.
    /// </exception>
    public async Task Handle(SetUserRolesCommand request, CancellationToken cancellationToken)
    {
        var requested = request.RoleNames.Distinct(StringComparer.Ordinal).ToArray();

        var roles = await _dbContext
            .Roles.Where(r => requested.Contains(r.Name))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var missing = requested.Except(roles.Select(r => r.Name), StringComparer.Ordinal).ToArray();
        if (missing.Length > 0)
        {
            throw new NotFoundException("Role", string.Join(", ", missing));
        }

        if (
            request.UserId == _currentUser.UserId
            && !requested.Contains(BuiltInRoles.Admin, StringComparer.Ordinal)
            && _currentUser.Roles.Contains(BuiltInRoles.Admin, StringComparer.Ordinal)
        )
        {
            throw new BusinessException(
                "You cannot remove your own administrator role. Ask another administrator."
            );
        }

        var existing = await _dbContext
            .UserRoles.Where(ur => ur.UserId == request.UserId)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        _dbContext.UserRoles.RemoveRange(existing);
        _dbContext.UserRoles.AddRange(roles.Select(r => UserRole.Grant(request.UserId, r.Id)));

        await _dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }
}
