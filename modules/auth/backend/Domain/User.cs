using EnterpriseFramework.Domain.Common;
using EnterpriseFramework.Modules.Auth.Contracts.Events;

namespace EnterpriseFramework.Modules.Auth.Domain;

/// <summary>
/// Account able to authenticate against the platform.
///
/// Owned by the Auth module: other modules never reference this entity — they
/// react to the public events in Auth.Contracts (e.g. <see cref="UserRegistered"/>) or,
/// from Fase 5, consume the Users module's contracts.
/// </summary>
public sealed class User : EntityBase<Guid>
{
    private User() { }

    /// <summary>Email address as entered by the user (original casing).</summary>
    public string Email { get; private set; } = string.Empty;

    /// <summary>
    /// Uppercase invariant email used for uniqueness and lookups, so
    /// "User@X.com" and "user@x.com" are the same account.
    /// </summary>
    public string NormalizedEmail { get; private set; } = string.Empty;

    /// <summary>Name shown in the UI.</summary>
    public string DisplayName { get; private set; } = string.Empty;

    /// <summary>Password hash (PBKDF2 via the framework hasher). Never the raw password.</summary>
    public string PasswordHash { get; private set; } = string.Empty;

    /// <summary>UTC instant the account was created.</summary>
    public DateTime CreatedAtUtc { get; private set; }

    /// <summary>
    /// Creates a new account and raises <see cref="UserRegistered"/>.
    ///
    /// Roles are deliberately NOT set here: the Authorization module grants
    /// them by subscribing to that event, so this module never holds a second
    /// copy of who may do what.
    /// </summary>
    /// <param name="email">Email address (already validated).</param>
    /// <param name="displayName">Name shown in the UI.</param>
    /// <param name="passwordHash">Hash produced by the password hasher.</param>
    /// <returns>The new account.</returns>
    public static User Register(string email, string displayName, string passwordHash)
    {
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = email,
            NormalizedEmail = email.ToUpperInvariant(),
            DisplayName = displayName,
            PasswordHash = passwordHash,
            CreatedAtUtc = DateTime.UtcNow,
        };
        user.RaiseDomainEvent(new UserRegistered(user.Id, user.Email, user.DisplayName));
        return user;
    }

    /// <summary>
    /// Replaces the password hash (used by password change/reset flows).
    /// </summary>
    /// <param name="passwordHash">The new hash.</param>
    public void SetPasswordHash(string passwordHash) => PasswordHash = passwordHash;
}
