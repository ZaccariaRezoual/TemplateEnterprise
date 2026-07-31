using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
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
///   (sub, email, name, roles) with the shared "Jwt" configuration, so the
///   host's bearer validation always accepts them.
/// - Generates cryptographically random refresh tokens and their SHA-256
///   hashes; only hashes are ever persisted.
///
/// Claims are prepared for Fase 5/7: roles are emitted per-role, and the
/// tenant claim is added here once Multi-Tenant lands.
/// </summary>
public sealed class TokenService
{
    private readonly JwtOptions _options;
    private readonly SigningCredentials _credentials;

    /// <summary>
    /// Initializes the service from the "Jwt" configuration.
    /// </summary>
    /// <param name="options">JWT options; the signing key must be configured.</param>
    /// <exception cref="InvalidOperationException">
    /// Thrown at startup when the signing key is missing or shorter than 32
    /// bytes — an unsigned or weakly-signed token must never be issuable.
    /// </exception>
    public TokenService(IOptions<JwtOptions> options)
    {
        _options = options.Value;

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
    /// Creates a signed access token for an account.
    /// </summary>
    /// <param name="user">The authenticated account.</param>
    /// <returns>The compact JWT and its expiry instant.</returns>
    public (string Token, DateTime ExpiresAtUtc) CreateAccessToken(User user)
    {
        var expires = DateTime.UtcNow.Add(AccessTokenLifetime);
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new(JwtRegisteredClaimNames.Email, user.Email),
            new(JwtRegisteredClaimNames.Name, user.DisplayName),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString("N")),
        };
        claims.AddRange(user.Roles.Select(role => new Claim(ClaimTypes.Role, role)));

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
