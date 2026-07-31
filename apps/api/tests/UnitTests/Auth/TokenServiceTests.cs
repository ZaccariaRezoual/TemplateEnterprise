using System.Security.Claims;
using EnterpriseFramework.Application.Abstractions;
using EnterpriseFramework.Modules.Auth.Domain;
using EnterpriseFramework.Modules.Auth.Options;
using EnterpriseFramework.Modules.Auth.Services;
using Microsoft.IdentityModel.JsonWebTokens;
using Shouldly;
using Xunit;

namespace EnterpriseFramework.UnitTests.Auth;

public sealed class TokenServiceTests
{
    /// <summary>Stub standing in for the Authorization module's enricher.</summary>
    private sealed class StubEnricher(params string[] permissions) : IUserClaimsEnricher
    {
        public Task<IReadOnlyCollection<Claim>> GetClaimsAsync(
            Guid userId,
            CancellationToken cancellationToken = default
        ) =>
            Task.FromResult<IReadOnlyCollection<Claim>>(
                [.. permissions.Select(p => new Claim(PermissionClaims.Permission, p))]
            );
    }

    private static TokenService CreateService(
        string? signingKey = null,
        params IUserClaimsEnricher[] enrichers
    ) =>
        new(
            Microsoft.Extensions.Options.Options.Create(
                new JwtOptions
                {
                    Issuer = "test-issuer",
                    Audience = "test-audience",
                    SigningKey = signingKey ?? new string('k', 48),
                    AccessTokenMinutes = 15,
                    RefreshTokenDays = 7,
                }
            ),
            enrichers
        );

    [Fact]
    public void Constructor_RefusesAMissingOrShortSigningKey()
    {
        Should.Throw<InvalidOperationException>(() => CreateService(signingKey: "short"));
        Should.Throw<InvalidOperationException>(() => CreateService(signingKey: ""));
    }

    [Fact]
    public async Task CreateAccessToken_EmitsIdentityClaims()
    {
        var user = User.Register("ada@example.com", "Ada Lovelace", "hash");
        var service = CreateService();

        var (token, expiresAtUtc) = await service.CreateAccessTokenAsync(user);

        var jwt = new JsonWebToken(token);
        jwt.Issuer.ShouldBe("test-issuer");
        jwt.Audiences.ShouldContain("test-audience");
        jwt.GetClaim(JwtRegisteredClaimNames.Sub).Value.ShouldBe(user.Id.ToString());
        jwt.GetClaim(JwtRegisteredClaimNames.Email).Value.ShouldBe("ada@example.com");
        expiresAtUtc.ShouldBeGreaterThan(DateTime.UtcNow.AddMinutes(14));
        expiresAtUtc.ShouldBeLessThan(DateTime.UtcNow.AddMinutes(16));
    }

    [Fact]
    public async Task CreateAccessToken_IncludesClaimsContributedByOtherModules()
    {
        var user = User.Register("ada@example.com", "Ada Lovelace", "hash");
        var service = CreateService(null, new StubEnricher("users.read", "users.write"));

        var (token, _) = await service.CreateAccessTokenAsync(user);

        var permissions = new JsonWebToken(token)
            .Claims.Where(c => c.Type == PermissionClaims.Permission)
            .Select(c => c.Value)
            .ToArray();
        permissions.ShouldBe(["users.read", "users.write"], ignoreOrder: true);
    }

    [Fact]
    public async Task CreateAccessToken_WorksWithNoEnricherRegistered()
    {
        // The Authorization module being disabled must not break token issuance.
        var user = User.Register("ada@example.com", "Ada Lovelace", "hash");
        var service = CreateService();

        var (token, _) = await service.CreateAccessTokenAsync(user);

        new JsonWebToken(token)
            .Claims.ShouldNotContain(c => c.Type == PermissionClaims.Permission);
    }

    [Fact]
    public void CreateRefreshToken_ProducesHighEntropyTokenAndMatchingHash()
    {
        var (raw, hash) = TokenService.CreateRefreshToken();

        raw.Length.ShouldBeGreaterThanOrEqualTo(43); // 32 bytes base64url
        hash.ShouldBe(TokenService.HashRefreshToken(raw));
        TokenService.CreateRefreshToken().RawToken.ShouldNotBe(raw);
    }
}
