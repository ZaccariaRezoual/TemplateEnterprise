using EnterpriseFramework.Modules.Auth.Domain;
using Microsoft.AspNetCore.Identity;

namespace EnterpriseFramework.Modules.Auth.Services;

/// <summary>
/// Default <see cref="IPasswordHasher"/> backed by ASP.NET Identity's
/// <see cref="PasswordHasher{TUser}"/> (PBKDF2-HMAC-SHA512, per-password salt,
/// versioned format). Chosen over hand-rolled crypto on purpose: it is
/// maintained, audited and supports transparent algorithm upgrades.
/// </summary>
public sealed class Pbkdf2PasswordHasher : IPasswordHasher
{
    // The Identity hasher wants a TUser instance for context; it never reads it.
    private static readonly User Placeholder = User.Register("hasher@internal", "hasher", "-");
    private readonly PasswordHasher<User> _inner = new();

    /// <inheritdoc />
    public string Hash(string password) => _inner.HashPassword(Placeholder, password);

    /// <inheritdoc />
    public bool Verify(string hash, string password) =>
        _inner.VerifyHashedPassword(Placeholder, hash, password)
            is PasswordVerificationResult.Success
                or PasswordVerificationResult.SuccessRehashNeeded;
}
