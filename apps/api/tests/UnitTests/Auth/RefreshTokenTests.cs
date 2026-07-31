using EnterpriseFramework.Modules.Auth.Domain;
using Shouldly;
using Xunit;

namespace EnterpriseFramework.UnitTests.Auth;

public sealed class RefreshTokenTests
{
    [Fact]
    public void Issue_CreatesAnActiveTokenWithTheRequestedLifetime()
    {
        var token = RefreshToken.Issue(Guid.NewGuid(), "hash", TimeSpan.FromDays(7));

        token.IsActive.ShouldBeTrue();
        token.ExpiresAtUtc.ShouldBeGreaterThan(DateTime.UtcNow.AddDays(6.9));
    }

    [Fact]
    public void IsActive_IsFalseOnceExpired()
    {
        var token = RefreshToken.Issue(Guid.NewGuid(), "hash", TimeSpan.FromMilliseconds(-1));

        token.IsActive.ShouldBeFalse();
    }

    [Fact]
    public void Revoke_DeactivatesAndRecordsTheSuccessor()
    {
        var token = RefreshToken.Issue(Guid.NewGuid(), "hash", TimeSpan.FromDays(7));

        token.Revoke("next-hash");

        token.IsActive.ShouldBeFalse();
        token.RevokedAtUtc.ShouldNotBeNull();
        token.ReplacedByTokenHash.ShouldBe("next-hash");
    }

    [Fact]
    public void Revoke_IsIdempotentAndKeepsTheFirstRevocation()
    {
        var token = RefreshToken.Issue(Guid.NewGuid(), "hash", TimeSpan.FromDays(7));
        token.Revoke("first");
        var firstRevokedAt = token.RevokedAtUtc;

        token.Revoke("second");

        token.RevokedAtUtc.ShouldBe(firstRevokedAt);
        token.ReplacedByTokenHash.ShouldBe("first");
    }
}
