using EnterpriseFramework.Api.Tenancy;
using Shouldly;
using Xunit;

namespace EnterpriseFramework.UnitTests.Enterprise;

public sealed class TenantContextTests
{
    private static TenantContext Create(bool enabled) =>
        new(Microsoft.Extensions.Options.Options.Create(new TenancyOptions { Enabled = enabled }));

    [Fact]
    public void SingleTenantDeploymentReportsNoTenant()
    {
        var context = Create(enabled: false);

        context.IsMultiTenant.ShouldBeFalse();
        context.TenantId.ShouldBeNull();
    }

    [Fact]
    public void RequireTenantId_ThrowsWhenUnresolved()
    {
        var context = Create(enabled: true);

        // Failing loudly beats writing a row with an empty tenant that then
        // belongs to nobody and shows up for everybody.
        Should.Throw<InvalidOperationException>(() => context.RequireTenantId());
    }

    [Fact]
    public void ResolveSetsTheTenantForTheRequest()
    {
        var context = Create(enabled: true);
        var tenantId = Guid.NewGuid();

        context.Resolve(tenantId);

        context.TenantId.ShouldBe(tenantId);
        context.RequireTenantId().ShouldBe(tenantId);
    }

    [Fact]
    public void TheTenantOfARequestCannotBeReassigned()
    {
        var context = Create(enabled: true);
        context.Resolve(Guid.NewGuid());

        // A service able to switch tenant mid-request is the shape every
        // cross-tenant leak takes.
        Should.Throw<InvalidOperationException>(() => context.Resolve(Guid.NewGuid()));
    }
}
