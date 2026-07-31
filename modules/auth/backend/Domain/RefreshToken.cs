using EnterpriseFramework.Domain.Common;

namespace EnterpriseFramework.Modules.Auth.Domain;

/// <summary>
/// Persisted refresh token.
///
/// Security properties:
/// - Only the SHA-256 hash of the token is stored; a database leak exposes no
///   usable credentials.
/// - Tokens rotate on every use: the presented token is revoked and replaced.
///   Presenting an already-rotated token is treated as theft (reuse detection)
///   and revokes the whole family.
/// </summary>
public sealed class RefreshToken : EntityBase<Guid>
{
    private RefreshToken() { }

    /// <summary>Account this token belongs to.</summary>
    public Guid UserId { get; private set; }

    /// <summary>SHA-256 hash of the raw token handed to the client.</summary>
    public string TokenHash { get; private set; } = string.Empty;

    /// <summary>UTC instant after which the token can no longer be used.</summary>
    public DateTime ExpiresAtUtc { get; private set; }

    /// <summary>UTC instant the token was issued.</summary>
    public DateTime CreatedAtUtc { get; private set; }

    /// <summary>UTC instant the token was revoked; null while active.</summary>
    public DateTime? RevokedAtUtc { get; private set; }

    /// <summary>Hash of the token that replaced this one on rotation, for auditing chains.</summary>
    public string? ReplacedByTokenHash { get; private set; }

    /// <summary>Whether the token can still be exchanged for a new pair.</summary>
    public bool IsActive => RevokedAtUtc is null && DateTime.UtcNow < ExpiresAtUtc;

    /// <summary>
    /// Issues a new refresh token for an account.
    /// </summary>
    /// <param name="userId">Owning account.</param>
    /// <param name="tokenHash">SHA-256 hash of the raw token.</param>
    /// <param name="lifetime">How long the token stays valid.</param>
    /// <returns>The persisted-side representation of the token.</returns>
    public static RefreshToken Issue(Guid userId, string tokenHash, TimeSpan lifetime)
    {
        var now = DateTime.UtcNow;
        return new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            TokenHash = tokenHash,
            CreatedAtUtc = now,
            ExpiresAtUtc = now.Add(lifetime),
        };
    }

    /// <summary>
    /// Revokes the token, optionally recording its successor.
    /// </summary>
    /// <param name="replacedByTokenHash">Hash of the rotated-in token, when rotating.</param>
    public void Revoke(string? replacedByTokenHash = null)
    {
        RevokedAtUtc ??= DateTime.UtcNow;
        ReplacedByTokenHash ??= replacedByTokenHash;
    }
}
