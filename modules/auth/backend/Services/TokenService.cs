using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using EnterpriseFramework.Application.Abstractions;
using EnterpriseFramework.Modules.Auth.Domain;
using EnterpriseFramework.Modules.Auth.Options;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace EnterpriseFramework.Modules.Auth.Services;

/// <summary>
/// Creates the token pair of a session.
///
/// Responsibilities:
/// - Signs short-lived JWT access tokens carrying identity claims
///   (sub, email, name) with the shared "Jwt" configuration, so the host's
///   bearer validation always accepts them.
/// - Invokes every registered <see cref="IUserClaimsEnricher"/> so other
///   modules can contribute claims (the Authorization module adds roles and
///   permissions) without this module referencing them.
/// - Generates cryptographically random refresh tokens and their SHA-256
///   hashes; only hashes are ever persisted.
/// </summary>
public sealed class TokenService
{
    private readonly JwtOptions _options;
    private readonly SigningCredentials _credentials;
    private readonly IEnumerable<IUserClaimsEnricher> _claimsEnrichers;

    /// <summary>
    /// Initializes the service from the "Jwt" configuration.
    /// </summary>
    /// <param name="options">JWT options; the signing key must be configured.</param>
    /// <param name="claimsEnrichers">
    /// Claim contributors discovered via DI; empty when no module registers one.
    /// </param>
    /// <exception cref="InvalidOperationException">
    /// Thrown at startup when the signing key is missing or shorter than 32
    /// bytes — an unsigned or weakly-signed token must never be issuable.
    /// </exception>
    public TokenService(
        IOptions<JwtOptions> options,
        IEnumerable<IUserClaimsEnricher> claimsEnrichers
    )
    {
        _options = options.Value;
        _claimsEnrichers = claimsEnrichers;

        if (Encoding.UTF8.GetByteCount(_options.SigningKey) < 32)
        {
            throw new InvalidOperationException(
                "Jwt:SigningKey is missing or shorter than 32 bytes. "
                    + "Configure it via user secrets or environment variables."
            );
        }

        _credentials = new SigningCredentials(
            new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_options.SigningKey)),
            SecurityAlgorithms.HmacSha256
        );
    }

    /// <summary>Access token lifetime, exposed for the API response.</summary>
    public TimeSpan AccessTokenLifetime => TimeSpan.FromMinutes(_options.AccessTokenMinutes);

    /// <summary>Refresh token lifetime.</summary>
    public TimeSpan RefreshTokenLifetime => TimeSpan.FromDays(_options.RefreshTokenDays);

    /// <summary>
    /// Creates a signed access token for an account, including any claims
    /// contributed by other modules.
    /// </summary>
    /// <param name="user">The authenticated account.</param>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    /// <returns>The compact JWT and its expiry instant.</returns>
    public async Task<(string Token, DateTime ExpiresAtUtc)> CreateAccessTokenAsync(
        User user,
        CancellationToken cancellationToken = default
    )
    {
        var expires = DateTime.UtcNow.Add(AccessTokenLifetime);
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new(JwtRegisteredClaimNames.Email, user.Email),
            new(JwtRegisteredClaimNames.Name, user.DisplayName),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString("N")),
        };

        foreach (var enricher in _claimsEnrichers)
        {
            claims.AddRange(
                await enricher.GetClaimsAsync(user.Id, cancellationToken).ConfigureAwait(false)
            );
        }

        var descriptor = new SecurityTokenDescriptor
        {
            Issuer = _options.Issuer,
            Audience = _options.Audience,
            Subject = new ClaimsIdentity(claims),
            Expires = expires,
            SigningCredentials = _credentials,
        };

        return (new JsonWebTokenHandler().CreateToken(descriptor), expires);
    }

    /// <summary>
    /// Generates a new refresh token.
    /// </summary>
    /// <returns>
    /// The raw token (256 bits, base64url — handed to the client once, never
    /// stored) and its SHA-256 hash (the only thing persisted).
    /// </returns>
    public static (string RawToken, string TokenHash) CreateRefreshToken()
    {
        var raw = Base64UrlEncoder.Encode(RandomNumberGenerator.GetBytes(32));
        return (raw, HashRefreshToken(raw));
    }

    /// <summary>
    /// Hashes a raw refresh token for storage or lookup.
    /// </summary>
    /// <param name="rawToken">The raw token presented by a client.</param>
    /// <returns>Base64 SHA-256 hash.</returns>
    public static string HashRefreshToken(string rawToken) =>
        Convert.ToBase64String(SHA256.HashData(Encoding.UTF8.GetBytes(rawToken)));
}
