using EnterpriseFramework.Modules.Authorization.Contracts;
using EnterpriseFramework.Modules.Authorization.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace EnterpriseFramework.Modules.Authorization.Features.Roles;

/// <summary>
/// Lists every defined role with its permissions. Used by the admin UI to
/// render role assignment.
/// </summary>
public sealed record ListRolesQuery : IRequest<IReadOnlyList<RoleDto>>;

/// <summary>
/// Handles <see cref="ListRolesQuery"/>.
/// </summary>
public sealed class ListRolesQueryHandler
    : IRequestHandler<ListRolesQuery, IReadOnlyList<RoleDto>>
{
    private readonly AuthorizationDbContext _dbContext;

    /// <summary>
    /// Initializes the handler.
    /// </summary>
    /// <param name="dbContext">Authorization persistence.</param>
    public ListRolesQueryHandler(AuthorizationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    /// <summary>
    /// Loads all roles, ordered by name.
    /// </summary>
    /// <param name="request">The query.</param>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    /// <returns>The roles.</returns>
    public async Task<IReadOnlyList<RoleDto>> Handle(
        ListRolesQuery request,
        CancellationToken cancellationToken
    )
    {
        var roles = await _dbContext
            .Roles.AsNoTracking()
            .OrderBy(r => r.Name)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return [.. roles.Select(RoleDto.FromRole)];
    }
}
