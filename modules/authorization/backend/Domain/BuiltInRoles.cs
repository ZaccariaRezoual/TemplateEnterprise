namespace EnterpriseFramework.Modules.Authorization.Domain;

/// <summary>
/// Roles the framework seeds on first run.
///
/// Two only, on purpose: a template that ships a dozen speculative roles
/// forces every project to delete them. Projects add their own.
/// </summary>
public static class BuiltInRoles
{
    /// <summary>Full access, including user and role administration.</summary>
    public const string Admin = "Admin";

    /// <summary>Default role granted to every new account: no admin permissions.</summary>
    public const string BasicUser = "BasicUser";

    /// <summary>
    /// Definitions applied by the seeder, idempotently, at startup.
    /// </summary>
    /// <returns>Name, description and permissions of each built-in role.</returns>
    public static IReadOnlyList<(string Name, string Description, string[] Permissions)> Definitions =>
        [
            (Admin, "Full access to every feature.", [.. Permissions.All]),
            (BasicUser, "Standard access without administration.", []),
        ];
}
