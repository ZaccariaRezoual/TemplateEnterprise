using EnterpriseFramework.Modules.Auth.Domain;
using EnterpriseFramework.Modules.Auth.Options;
using EnterpriseFramework.Modules.Auth.Services;
using Microsoft.IdentityModel.JsonWebTokens;
using Shouldly;
using Xunit;

namespace EnterpriseFramework.UnitTests.Auth;

public sealed class TokenServiceTests
{
    private static TokenService CreateService(string? signingKey = null) =>
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
            )
        );

    [Fact]
    public void Constructor_RefusesAMissingOrShortSigningKey()
    {
        Should.Throw<InvalidOperationException>(() => CreateService(signingKey: "short"));
        Should.Throw<InvalidOperationException>(() => CreateService(signingKey: ""));
    }

    [Fact]
    public void CreateAccessToken_EmitsIdentityAndRoleClaims()
    {
        var user = User.Register("ada@example.com", "Ada Lovelace", "hash");
        var service = CreateService();

        var (token, expiresAtUtc) = service.CreateAccessToken(user);

        var jwt = new JsonWebToken(token);
        jwt.Issuer.ShouldBe("test-issuer");
        jwt.Audiences.ShouldContain("test-audience");
        jwt.GetClaim(JwtRegisteredClaimNames.Sub).Value.ShouldBe(user.Id.ToString());
        jwt.GetClaim(JwtRegisteredClaimNames.Email).Value.ShouldBe("ada@example.com");
        jwt.Claims.Where(c => c.Type is "role" or System.Security.Claims.ClaimTypes.Role)
            .Select(c => c.Value)
            .ShouldContain("User");
        expiresAtUtc.ShouldBeGreaterThan(DateTime.UtcNow.AddMinutes(14));
        expiresAtUtc.ShouldBeLessThan(DateTime.UtcNow.AddMinutes(16));
    }

    [Fact]
    public void CreateRefreshToken_ProducesHighEntropyTokenAndMatchingHash()
    {
        var (raw, hash) = TokenService.CreateRefreshToken();

        raw.Length.ShouldBeGreaterThanOrEqualTo(43); // 32 bytes base64url
        hash.ShouldBe(TokenService.HashRefreshToken(raw));
        // Two tokens must never collide.
        TokenService.CreateRefreshToken().RawToken.ShouldNotBe(raw);
    }
}
