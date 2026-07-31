using EnterpriseFramework.Domain.Common;

namespace EnterpriseFramework.Modules.Authorization.Domain;

/// <summary>
/// Assignment of a role to an account.
///
/// Referenced by user id only: this module owns no user table, and the
/// account itself belongs to the Auth module. Cross-module references are
/// identifiers, never foreign keys into another module's schema — that is
/// what keeps a module removable.
/// </summary>
public sealed class UserRole : EntityBase<Guid>
{
    private UserRole() { }

    /// <summary>Account the role is granted to.</summary>
    public Guid UserId { get; private set; }

    /// <summary>Role granted.</summary>
    public Guid RoleId { get; private set; }

    /// <summary>UTC instant the role was granted.</summary>
    public DateTime GrantedAtUtc { get; private set; }

    /// <summary>
    /// Grants a role to an account.
    /// </summary>
    /// <param name="userId">Account receiving the role.</param>
    /// <param name="roleId">Role granted.</param>
    /// <returns>The assignment.</returns>
    public static UserRole Grant(Guid userId, Guid roleId) =>
        new()
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            RoleId = roleId,
            GrantedAtUtc = DateTime.UtcNow,
        };
}
