namespace EnterpriseFramework.Modules.Auth.Options;

/// <summary>
/// Configuration of the bootstrap administrator account, seeded at startup so
/// a fresh installation is usable without touching the database by hand.
///
/// Bound from "Modules:Auth:BootstrapAdmin". Disabled outside Development
/// unless explicitly enabled: an account with a known password is a way in for
/// anyone who has read this repository, and this repository is a template that
/// will be copied verbatim.
/// </summary>
public sealed class BootstrapAdminOptions
{
    /// <summary>Configuration section binding these options.</summary>
    public const string SectionName = "Modules:Auth:BootstrapAdmin";

    /// <summary>
    /// Whether to seed the account. Defaults to true in Development only;
    /// a production deployment that wants it must say so, and should change
    /// <see cref="Password"/> at the same time.
    /// </summary>
    public bool Enabled { get; init; }

    /// <summary>Email used to sign in. Also the account's unique key.</summary>
    public string Email { get; init; } = "admin@example.com";

    /// <summary>Name shown in the UI.</summary>
    public string DisplayName { get; init; } = "Admin";

    /// <summary>
    /// Password of the seeded account. It must satisfy the same policy as
    /// registration (12+ characters, upper, lower and a digit), otherwise the
    /// seeder refuses to create an account nobody could recreate through the
    /// normal flow.
    /// </summary>
    public string Password { get; init; } = "Password123!";
}
