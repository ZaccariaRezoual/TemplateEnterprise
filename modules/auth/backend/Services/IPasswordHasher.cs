namespace EnterpriseFramework.Modules.Auth.Services;

/// <summary>
/// Hashes and verifies passwords.
///
/// Abstraction over ASP.NET Identity's PBKDF2 hasher so use cases stay
/// testable and the algorithm can be upgraded (the stored format is versioned
/// by the underlying implementation).
/// </summary>
public interface IPasswordHasher
{
    /// <summary>
    /// Hashes a raw password with a per-password salt.
    /// </summary>
    /// <param name="password">The raw password.</param>
    /// <returns>The self-describing hash string to persist.</returns>
    string Hash(string password);

    /// <summary>
    /// Verifies a raw password against a stored hash.
    /// </summary>
    /// <param name="hash">The stored hash.</param>
    /// <param name="password">The raw password to check.</param>
    /// <returns><c>true</c> when the password matches.</returns>
    bool Verify(string hash, string password);
}
