namespace EnterpriseFramework.Modules.Authorization.Domain;

/// <summary>
/// Catalogue of permissions known to the framework.
///
/// Permissions are named "resource.action" and are the ONLY thing endpoints
/// and UI check against — never a role name. Roles are a grouping convenience
/// that customers reorganize; permissions are the stable contract, so
/// "can this user delete a user?" never becomes "is this user an Admin?".
///
/// Modules add their own constants here (or contribute them at registration,
/// once a module marketplace exists in Fase 7).
/// </summary>
public static class Permissions
{
    /// <summary>Permissions over user accounts.</summary>
    public static class Users
    {
        /// <summary>List and view user accounts.</summary>
        public const string Read = "users.read";

        /// <summary>Create and update user accounts.</summary>
        public const string Write = "users.write";

        /// <summary>Delete user accounts.</summary>
        public const string Delete = "users.delete";
    }

    /// <summary>Permissions over roles and permission assignments.</summary>
    public static class Roles
    {
        /// <summary>View roles and their permissions.</summary>
        public const string Read = "roles.read";

        /// <summary>Assign and revoke roles.</summary>
        public const string Write = "roles.write";
    }

    /// <summary>Permissions over the audit trail.</summary>
    public static class Audit
    {
        /// <summary>Read the audit trail.</summary>
        public const string Read = "audit.read";
    }

    /// <summary>Permissions over installation-wide settings.</summary>
    public static class Settings
    {
        /// <summary>Change settings that affect every user.</summary>
        public const string Write = "settings.write";
    }

    /// <summary>Permissions over stored files.</summary>
    public static class Files
    {
        /// <summary>Upload files.</summary>
        public const string Write = "files.write";

        /// <summary>Delete files uploaded by anyone.</summary>
        public const string Delete = "files.delete";
    }

    /// <summary>Every permission the framework ships with, used for seeding.</summary>
    public static IReadOnlyList<string> All { get; } =
        [
            Users.Read,
            Users.Write,
            Users.Delete,
            Roles.Read,
            Roles.Write,
            Audit.Read,
            Settings.Write,
            Files.Write,
            Files.Delete,
        ];
}
