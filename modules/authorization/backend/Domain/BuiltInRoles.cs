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
    public const string Administrator = "Administrator";

    /// <summary>Default role granted to every new account: no admin permissions.</summary>
    public const string User = "User";

    /// <summary>
    /// Definitions applied by the seeder, idempotently, at startup.
    /// </summary>
    /// <returns>Name, description and permissions of each built-in role.</returns>
    public static IReadOnlyList<(string Name, string Description, string[] Permissions)> Definitions =>
        [
            (Administrator, "Full access to every feature.", [.. Permissions.All]),
            (User, "Standard access without administration.", []),
        ];
}
