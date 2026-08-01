using EnterpriseFramework.Api.Features;
using EnterpriseFramework.Application.Abstractions;
using Microsoft.Extensions.Options;
using Shouldly;
using Xunit;

namespace EnterpriseFramework.UnitTests.Enterprise;

public sealed class FeatureFlagsTests
{
    private sealed class StubUser(Guid? userId) : ICurrentUser
    {
        public bool IsAuthenticated => userId is not null;

        public Guid? UserId => userId;

        public IReadOnlyList<string> Roles => [];

        public IReadOnlyList<string> Permissions => [];
    }

    private sealed class StubTenant(Guid? tenantId) : ITenantContext
    {
        public bool IsMultiTenant => tenantId is not null;

        public Guid? TenantId => tenantId;

        public Guid RequireTenantId() => tenantId ?? throw new InvalidOperationException();
    }

    private static ConfigurationFeatureFlags Create(
        Dictionary<string, FeatureFlagDefinition> flags,
        Guid? userId = null,
        Guid? tenantId = null
    ) =>
        new(
            new StubOptionsMonitor(new FeatureFlagOptions { Flags = flags }),
            new StubUser(userId),
            new StubTenant(tenantId)
        );

    private sealed class StubOptionsMonitor(FeatureFlagOptions value)
        : IOptionsMonitor<FeatureFlagOptions>
    {
        public FeatureFlagOptions CurrentValue => value;

        public FeatureFlagOptions Get(string? name) => value;

        public IDisposable? OnChange(Action<FeatureFlagOptions, string?> listener) => null;
    }

    [Fact]
    public async Task UnknownFlagIsOff()
    {
        var flags = Create([]);

        // A typo must not silently enable something, and a flag deleted from
        // configuration must not resurrect the code path it guarded.
        (await flags.IsEnabledAsync("does.not.exist")).ShouldBeFalse();
    }

    [Fact]
    public async Task DisabledFlagIsOff()
    {
        var flags = Create(new() { ["beta"] = new FeatureFlagDefinition { Enabled = false } });

        (await flags.IsEnabledAsync("beta")).ShouldBeFalse();
    }

    [Fact]
    public async Task EnabledFlagIsOnForEveryone()
    {
        var flags = Create(new() { ["beta"] = new FeatureFlagDefinition { Enabled = true } });

        (await flags.IsEnabledAsync("beta")).ShouldBeTrue();
    }

    [Fact]
    public async Task PerUserAllowListOverridesTheDefault()
    {
        var userId = Guid.NewGuid();
        var flags = Create(
            new()
            {
                ["beta"] = new FeatureFlagDefinition { Enabled = false, EnabledForUsers = [userId] },
            },
            userId: userId
        );

        (await flags.IsEnabledAsync("beta")).ShouldBeTrue();
    }

    [Fact]
    public async Task PerUserAllowListDoesNotLeakToOtherUsers()
    {
        var flags = Create(
            new()
            {
                ["beta"] = new FeatureFlagDefinition
                {
                    Enabled = false,
                    EnabledForUsers = [Guid.NewGuid()],
                },
            },
            userId: Guid.NewGuid()
        );

        (await flags.IsEnabledAsync("beta")).ShouldBeFalse();
    }

    [Fact]
    public async Task PerTenantAllowListOverridesTheDefault()
    {
        var tenantId = Guid.NewGuid();
        var flags = Create(
            new()
            {
                ["beta"] = new FeatureFlagDefinition
                {
                    Enabled = false,
                    EnabledForTenants = [tenantId],
                },
            },
            tenantId: tenantId
        );

        (await flags.IsEnabledAsync("beta")).ShouldBeTrue();
    }

    [Fact]
    public async Task FlagNamesAreCaseInsensitive()
    {
        var flags = Create(
            new(StringComparer.OrdinalIgnoreCase)
            {
                ["Beta.NewDashboard"] = new FeatureFlagDefinition { Enabled = true },
            }
        );

        (await flags.IsEnabledAsync("beta.newdashboard")).ShouldBeTrue();
    }

    [Fact]
    public async Task GetAllEvaluatesEveryFlagForTheCaller()
    {
        var userId = Guid.NewGuid();
        var flags = Create(
            new()
            {
                ["on"] = new FeatureFlagDefinition { Enabled = true },
                ["off"] = new FeatureFlagDefinition { Enabled = false },
                ["preview"] = new FeatureFlagDefinition
                {
                    Enabled = false,
                    EnabledForUsers = [userId],
                },
            },
            userId: userId
        );

        var all = await flags.GetAllAsync();

        all["on"].ShouldBeTrue();
        all["off"].ShouldBeFalse();
        all["preview"].ShouldBeTrue();
    }
}
