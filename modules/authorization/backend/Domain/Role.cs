using EnterpriseFramework.Domain.Common;

namespace EnterpriseFramework.Modules.Authorization.Domain;

/// <summary>
/// Named group of permissions granted together.
///
/// Roles exist for administration convenience; authorization decisions are
/// always made on PERMISSIONS, so reorganizing roles never requires touching
/// an endpoint or a component.
/// </summary>
public sealed class Role : EntityBase<Guid>
{
    private readonly List<string> _permissions = [];

    private Role() { }

    /// <summary>Unique role name (e.g. "Admin").</summary>
    public string Name { get; private set; } = string.Empty;

    /// <summary>What the role is for, shown in the admin UI.</summary>
    public string Description { get; private set; } = string.Empty;

    /// <summary>
    /// Whether the role ships with the framework. Built-in roles cannot be
    /// deleted: removing "Admin" would be an unrecoverable lock-out.
    /// </summary>
    public bool IsBuiltIn { get; private set; }

    /// <summary>Permissions granted by this role.</summary>
    public IReadOnlyList<string> Permissions => _permissions.AsReadOnly();

    /// <summary>
    /// Creates a role.
    /// </summary>
    /// <param name="name">Unique name.</param>
    /// <param name="description">Purpose of the role.</param>
    /// <param name="permissions">Permissions granted.</param>
    /// <param name="isBuiltIn">Whether the framework owns this role.</param>
    /// <returns>The new role.</returns>
    public static Role Create(
        string name,
        string description,
        IEnumerable<string> permissions,
        bool isBuiltIn = false
    )
    {
        var role = new Role
        {
            Id = Guid.NewGuid(),
            Name = name,
            Description = description,
            IsBuiltIn = isBuiltIn,
        };
        role._permissions.AddRange(permissions.Distinct(StringComparer.Ordinal));
        return role;
    }

    /// <summary>
    /// Replaces the granted permissions.
    /// </summary>
    /// <param name="permissions">The new permission set.</param>
    public void SetPermissions(IEnumerable<string> permissions)
    {
        _permissions.Clear();
        _permissions.AddRange(permissions.Distinct(StringComparer.Ordinal));
    }
}
