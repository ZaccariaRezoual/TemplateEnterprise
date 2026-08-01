using EnterpriseFramework.Application.Abstractions;
using EnterpriseFramework.Modules.Dashboard;
using Microsoft.Extensions.Logging.Abstractions;
using Shouldly;
using Xunit;

namespace EnterpriseFramework.UnitTests.Dashboard;

public sealed class DashboardWidgetAggregatorTests
{
    /// <summary>Provider returning a fixed set of tiles.</summary>
    private sealed class StubProvider(params DashboardWidget[] widgets) : IDashboardWidgetProvider
    {
        public Task<IReadOnlyList<DashboardWidget>> GetWidgetsAsync(
            CancellationToken cancellationToken = default
        ) => Task.FromResult<IReadOnlyList<DashboardWidget>>(widgets);
    }

    /// <summary>Provider standing in for a module whose backing store is down.</summary>
    private sealed class FailingProvider : IDashboardWidgetProvider
    {
        public Task<IReadOnlyList<DashboardWidget>> GetWidgetsAsync(
            CancellationToken cancellationToken = default
        ) => throw new InvalidOperationException("database unreachable");
    }

    private static DashboardWidgetAggregator Aggregator(
        params IDashboardWidgetProvider[] providers
    ) => new(providers, NullLogger<DashboardWidgetAggregator>.Instance);

    [Fact]
    public async Task WithoutProviders_ReturnsNothing()
    {
        var widgets = await Aggregator().GetWidgetsAsync();

        widgets.ShouldBeEmpty();
    }

    [Fact]
    public async Task CollectsTilesFromEveryProvider()
    {
        var aggregator = Aggregator(
            new StubProvider(new DashboardWidget("users.total", "Users")),
            new StubProvider(new DashboardWidget("audit.recent", "Recent activity"))
        );

        var widgets = await aggregator.GetWidgetsAsync();

        widgets.Select(widget => widget.Id).ShouldBe(["users.total", "audit.recent"], ignoreOrder: true);
    }

    [Fact]
    public async Task OrdersByOrderThenTitle()
    {
        var aggregator = Aggregator(
            new StubProvider(
                new DashboardWidget("c", "Beta", Order: 20),
                new DashboardWidget("a", "Alpha", Order: 20),
                new DashboardWidget("b", "Zulu", Order: 10)
            )
        );

        var widgets = await aggregator.GetWidgetsAsync();

        // Order wins over the alphabet; the title is only the tie-break, so the
        // grid stays stable across requests instead of shuffling.
        widgets.Select(widget => widget.Id).ShouldBe(["b", "a", "c"]);
    }

    [Fact]
    public async Task AFailingProvider_DoesNotBlankTheDashboard()
    {
        var aggregator = Aggregator(
            new FailingProvider(),
            new StubProvider(new DashboardWidget("users.total", "Users"))
        );

        var widgets = await aggregator.GetWidgetsAsync();

        // The healthy module's tile still renders: a partial dashboard beats an
        // error page, which is the whole reason providers are isolated.
        widgets.Select(widget => widget.Id).ShouldBe(["users.total"]);
    }

    [Fact]
    public async Task Cancellation_IsNotSwallowedAsAProviderFailure()
    {
        using var cancellation = new CancellationTokenSource();
        await cancellation.CancelAsync();

        var aggregator = Aggregator(new CancelAwareProvider());

        // A cancelled request must surface as cancellation, not be logged as a
        // broken module and silently produce a half-empty response.
        await Should.ThrowAsync<OperationCanceledException>(
            aggregator.GetWidgetsAsync(cancellation.Token)
        );
    }

    /// <summary>Provider that honours the cancellation token, as providers should.</summary>
    private sealed class CancelAwareProvider : IDashboardWidgetProvider
    {
        public Task<IReadOnlyList<DashboardWidget>> GetWidgetsAsync(
            CancellationToken cancellationToken = default
        )
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult<IReadOnlyList<DashboardWidget>>([]);
        }
    }
}
