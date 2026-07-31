using EnterpriseFramework.Modules.Authorization.Domain;

namespace EnterpriseFramework.Modules.Authorization.Contracts;

/// <summary>
/// Role as exposed to clients.
/// </summary>
/// <param name="Id">Role identifier.</param>
/// <param name="Name">Unique role name.</param>
/// <param name="Description">What the role is for.</param>
/// <param name="IsBuiltIn">Whether the framework owns the role (not deletable).</param>
/// <param name="Permissions">Permissions granted by the role.</param>
public sealed record RoleDto(
    Guid Id,
    string Name,
    string Description,
    bool IsBuiltIn,
    IReadOnlyList<string> Permissions
)
{
    /// <summary>
    /// Maps a <see cref="Role"/> entity to its client representation.
    /// </summary>
    /// <param name="role">The entity to map.</param>
    /// <returns>The DTO.</returns>
    public static RoleDto FromRole(Role role) =>
        new(role.Id, role.Name, role.Description, role.IsBuiltIn, role.Permissions);
}

/// <summary>
/// Effective authorization of an account: its roles and the union of their
/// permissions. Returned by the "my permissions" endpoint the frontend uses
/// to drive the UI.
/// </summary>
/// <param name="Roles">Role names granted to the account.</param>
/// <param name="Permissions">Every permission the account effectively holds.</param>
public sealed record UserAuthorizationDto(
    IReadOnlyList<string> Roles,
    IReadOnlyList<string> Permissions
);
