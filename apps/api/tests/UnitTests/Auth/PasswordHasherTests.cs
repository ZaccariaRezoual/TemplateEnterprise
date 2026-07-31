using EnterpriseFramework.Modules.Auth.Services;
using Shouldly;
using Xunit;

namespace EnterpriseFramework.UnitTests.Auth;

public sealed class PasswordHasherTests
{
    private readonly Pbkdf2PasswordHasher _hasher = new();

    [Fact]
    public void Hash_NeverStoresThePasswordAndSaltsPerCall()
    {
        var first = _hasher.Hash("Str0ngPassphrase!");
        var second = _hasher.Hash("Str0ngPassphrase!");

        first.ShouldNotContain("Str0ngPassphrase!");
        // Same password, different salt, different hash.
        first.ShouldNotBe(second);
    }

    [Fact]
    public void Verify_AcceptsTheCorrectPasswordAndRejectsOthers()
    {
        var hash = _hasher.Hash("Str0ngPassphrase!");

        _hasher.Verify(hash, "Str0ngPassphrase!").ShouldBeTrue();
        _hasher.Verify(hash, "WrongPassphrase1").ShouldBeFalse();
    }
}
