using System.Security.Claims;
using EnterpriseFramework.Application.Abstractions;

namespace EnterpriseFramework.Modules.Authorization.Services;

/// <summary>
/// Adds role and permission claims to the access tokens the Auth module
/// issues, through the <see cref="IUserClaimsEnricher"/> extension point.
///
/// Permissions are embedded in the token so authorization is a stateless
/// check on the API side — no database round-trip per request. The trade-off
/// is staleness: a revoked permission stays effective until the access token
/// expires (15 minutes by default) or the client refreshes. Flows that cannot
/// tolerate that window must re-check against the database in the handler.
/// </summary>
public sealed class RoleClaimsEnricher : IUserClaimsEnricher
{
    private readonly PermissionReader _permissionReader;

    /// <summary>
    /// Initializes the enricher.
    /// </summary>
    /// <param name="permissionReader">Resolves effective roles and permissions.</param>
    public RoleClaimsEnricher(PermissionReader permissionReader)
    {
        _permissionReader = permissionReader;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyCollection<Claim>> GetClaimsAsync(
        Guid userId,
        CancellationToken cancellationToken = default
    )
    {
        var (roles, permissions) = await _permissionReader
            .GetForUserAsync(userId, cancellationToken)
            .ConfigureAwait(false);

        return
        [
            .. roles.Select(role => new Claim(ClaimTypes.Role, role)),
            .. permissions.Select(permission => new Claim(PermissionClaims.Permission, permission)),
        ];
    }
}
