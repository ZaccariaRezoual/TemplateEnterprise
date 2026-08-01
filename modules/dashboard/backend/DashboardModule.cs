using EnterpriseFramework.Application.Abstractions;
using EnterpriseFramework.Modules.Abstractions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace EnterpriseFramework.Modules.Dashboard;

/// <summary>
/// Dashboard module: one endpoint that returns the tiles every installed
/// module contributed.
///
/// It is the framework's composition made visible. The module has no
/// knowledge of Users, Audit, Notifications or Realtime; it asks the
/// container for every <see cref="IDashboardWidgetProvider"/> and renders
/// whatever comes back. Disable a module and its tiles disappear with it.
///
/// The module contains no aggregation logic: that lives in
/// <see cref="DashboardWidgetAggregator"/>.
/// </summary>
public sealed class DashboardModule : IModule
{
    /// <inheritdoc />
    public string Name => "Dashboard";

    /// <inheritdoc />
    public void ConfigureServices(IServiceCollection services, IConfiguration configuration)
    {
        // Only the aggregator: the providers belong to the modules that
        // contribute them, and each registers its own.
        services.AddScoped<DashboardWidgetAggregator>();
    }

    /// <inheritdoc />
    public void MapEndpoints(IEndpointRouteBuilder endpoints)
    {
        endpoints
            .MapGet("/api/dashboard/widgets", GetWidgetsAsync)
            .WithName("dashboardWidgets")
            .WithSummary("Returns the dashboard tiles visible to the caller.")
            .WithTags("Dashboard");
    }

    // The concrete `Ok<T>` return type, not `IResult`: it is what carries the
    // response shape into the OpenAPI document and therefore into the
    // generated SDK. `IResult` erases it and the client ends up untyped.
    private static async Task<Ok<DashboardWidget[]>> GetWidgetsAsync(
        DashboardWidgetAggregator aggregator,
        CancellationToken cancellationToken
    ) => TypedResults.Ok(await aggregator.GetWidgetsAsync(cancellationToken).ConfigureAwait(false));
}
