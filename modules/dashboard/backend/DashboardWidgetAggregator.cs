using EnterpriseFramework.Application.Abstractions;
using Microsoft.Extensions.Logging;

namespace EnterpriseFramework.Modules.Dashboard;

/// <summary>
/// Collects the tiles contributed by every installed module and returns them
/// in display order.
///
/// Responsibilities: querying each <see cref="IDashboardWidgetProvider"/>,
/// isolating a provider that fails, and ordering the result.
/// It does not decide what a tile contains and does not check permissions —
/// each provider owns both for its own module.
/// </summary>
public sealed partial class DashboardWidgetAggregator
{
    private readonly IEnumerable<IDashboardWidgetProvider> _providers;
    private readonly ILogger<DashboardWidgetAggregator> _logger;

    /// <summary>
    /// Creates the aggregator.
    /// </summary>
    /// <param name="providers">Every widget provider registered in the container.</param>
    /// <param name="logger">Logger used to report a failing provider.</param>
    public DashboardWidgetAggregator(
        IEnumerable<IDashboardWidgetProvider> providers,
        ILogger<DashboardWidgetAggregator> logger
    )
    {
        _providers = providers;
        _logger = logger;
    }

    /// <summary>
    /// Returns every tile the caller may see, ordered by
    /// <see cref="DashboardWidget.Order"/> and then by title.
    /// </summary>
    /// <param name="cancellationToken">Cancels the pending provider calls.</param>
    /// <returns>The ordered tiles; empty when no module contributed one.</returns>
    public async Task<DashboardWidget[]> GetWidgetsAsync(
        CancellationToken cancellationToken = default
    )
    {
        var widgets = new List<DashboardWidget>();

        foreach (var provider in _providers)
        {
            try
            {
                widgets.AddRange(
                    await provider.GetWidgetsAsync(cancellationToken).ConfigureAwait(false)
                );
            }
            catch (Exception exception) when (exception is not OperationCanceledException)
            {
                // One failing module must not blank the whole dashboard: the
                // other tiles are still useful, and a missing tile is a far
                // better outcome than an error page.
                LogProviderFailure(_logger, provider.GetType().Name, exception);
            }
        }

        // Ordinal comparison: the tie-break only has to be STABLE, and a
        // culture-sensitive one would reorder tiles between servers.
        return
        [
            .. widgets
                .OrderBy(widget => widget.Order)
                .ThenBy(widget => widget.Title, StringComparer.Ordinal),
        ];
    }

    [LoggerMessage(
        Level = LogLevel.Warning,
        Message = "Dashboard provider {Provider} failed; its tiles are omitted"
    )]
    private static partial void LogProviderFailure(
        ILogger logger,
        string provider,
        Exception exception
    );
}
