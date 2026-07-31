using EnterpriseFramework.Modules.Authorization.Domain;
using EnterpriseFramework.Modules.Authorization.Persistence;
using Microsoft.EntityFrameworkCore;

namespace EnterpriseFramework.Modules.Authorization.Services;

/// <summary>
/// Resolves the effective roles and permissions of an account.
///
/// "Effective" means the union across every assigned role: permissions are
/// additive and there are no explicit denies, because deny rules make
/// "why can't this user do X?" unanswerable without a debugger.
/// </summary>
public sealed class PermissionReader
{
    private readonly AuthorizationDbContext _dbContext;

    /// <summary>
    /// Initializes the reader.
    /// </summary>
    /// <param name="dbContext">Authorization persistence.</param>
    public PermissionReader(AuthorizationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    /// <summary>
    /// Loads the roles and permissions granted to an account.
    /// </summary>
    /// <param name="userId">Account to inspect.</param>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    /// <returns>Role names and the union of their permissions, both sorted.</returns>
    public async Task<(IReadOnlyList<string> Roles, IReadOnlyList<string> Permissions)> GetForUserAsync(
        Guid userId,
        CancellationToken cancellationToken = default
    )
    {
        var roles = await _dbContext
            .UserRoles.AsNoTracking()
            .Where(ur => ur.UserId == userId)
            .Join(_dbContext.Roles.AsNoTracking(), ur => ur.RoleId, r => r.Id, (_, r) => r)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var roleNames = roles.Select(r => r.Name).Order(StringComparer.Ordinal).ToArray();
        var permissions = roles
            .SelectMany(r => r.Permissions)
            .Distinct(StringComparer.Ordinal)
            .Order(StringComparer.Ordinal)
            .ToArray();

        return (roleNames, permissions);
    }

    /// <summary>
    /// Finds a role by name.
    /// </summary>
    /// <param name="name">Role name.</param>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    /// <returns>The role, or null when it does not exist.</returns>
    public Task<Role?> FindRoleByNameAsync(string name, CancellationToken cancellationToken = default) =>
        _dbContext.Roles.SingleOrDefaultAsync(r => r.Name == name, cancellationToken);
}
