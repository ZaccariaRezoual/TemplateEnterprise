using System.Globalization;
using EnterpriseFramework.Api.Extensions;
using EnterpriseFramework.Api.Middleware;
using EnterpriseFramework.Application;
using EnterpriseFramework.Infrastructure;
using EnterpriseFramework.Modules.Abstractions;
using EnterpriseFramework.Modules.Demo;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Serilog;

// Bootstrap logger: catches fatal startup errors before the full
// configuration-driven logger is available.
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console(formatProvider: CultureInfo.InvariantCulture)
    .CreateBootstrapLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);

    builder.WebHost.ConfigureKestrel(kestrel => kestrel.AddServerHeader = false);
    builder.Host.UseSerilog(
        (context, loggerConfiguration) =>
            loggerConfiguration.ReadFrom.Configuration(context.Configuration)
    );

    builder.Services.AddApplication();
    builder.Services.AddInfrastructure(builder.Configuration);
    builder.Services.AddApiServices(builder.Configuration);

    // Module system: list module assemblies here; the loader reads each
    // embedded module.json, honors enabled flags and resolves dependency order.
    var modules = ModuleLoader.Load([typeof(DemoModule).Assembly], builder.Configuration);
    foreach (var module in modules)
    {
        module.Instance.ConfigureServices(builder.Services, builder.Configuration);
    }

    var app = builder.Build();

    app.UseExceptionHandler();
    app.UseMiddleware<CorrelationIdMiddleware>();
    app.UseMiddleware<SecurityHeadersMiddleware>();
    app.UseSerilogRequestLogging();
    app.UseCors(ApiServiceCollectionExtensions.CorsPolicyName);
    app.UseRateLimiter();

    app.MapOpenApi();

    // Liveness = process is up; readiness = external dependencies reachable.
    app.MapHealthChecks("/health/live", new HealthCheckOptions { Predicate = _ => false });
    app.MapHealthChecks(
        "/health/ready",
        new HealthCheckOptions { Predicate = check => check.Tags.Contains("ready") }
    );
    app.MapHealthChecks("/health");

    foreach (var module in modules)
    {
        module.Instance.MapEndpoints(app);
        Log.Information(
            "Module {ModuleName} v{ModuleVersion} loaded",
            module.Manifest.Name,
            module.Manifest.Version
        );
    }

    app.Run();
}
catch (Exception exception)
{
    Log.Fatal(exception, "API terminated unexpectedly at startup");
    throw;
}
finally
{
    Log.CloseAndFlush();
}

/// <summary>
/// Marker partial class making the entry point visible to
/// <c>WebApplicationFactory&lt;Program&gt;</c> in integration tests.
/// </summary>
public partial class Program;
