using System.Globalization;
using EnterpriseFramework.Api.Extensions;
using EnterpriseFramework.Api.Features;
using EnterpriseFramework.Api.Observability;
using EnterpriseFramework.Api.Middleware;
using EnterpriseFramework.Api.Security;
using EnterpriseFramework.Api.Tenancy;
using EnterpriseFramework.Application;
using EnterpriseFramework.Infrastructure;
using EnterpriseFramework.Modules.Abstractions;
using EnterpriseFramework.Modules.Audit;
using EnterpriseFramework.Modules.Auth;
using EnterpriseFramework.Modules.Authorization;
using EnterpriseFramework.Modules.Dashboard;
using EnterpriseFramework.Modules.Demo;
using EnterpriseFramework.Modules.Email;
using EnterpriseFramework.Modules.Localization;
using EnterpriseFramework.Modules.Notifications;
using EnterpriseFramework.Modules.Realtime;
using EnterpriseFramework.Modules.Settings;
using EnterpriseFramework.Modules.Storage;
using EnterpriseFramework.Modules.Users;
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
    // preserveStaticLogger: each host builds its OWN logger instead of
    // freezing the static bootstrap one — required to boot several hosts in
    // one process (integration tests) and harmless in production.
    builder.Host.UseSerilog(
        (context, loggerConfiguration) =>
            loggerConfiguration.ReadFrom.Configuration(context.Configuration),
        preserveStaticLogger: true
    );

    // Global default for module auto-migration: ON in development, OFF
    // everywhere else. Production applies migrations as an explicit release
    // step, and this also keeps `dotnet build` (which boots the host to emit
    // the OpenAPI document) from requiring a reachable database.
    // A module may still override it with "Modules:<Name>:AutoMigrate".
    builder.Configuration["Modules:AutoMigrate"] ??= builder
        .Environment.IsDevelopment()
        .ToString();

    builder.Services.AddApplication();
    builder.Services.AddInfrastructure(builder.Configuration);
    builder.Services.AddApiServices(builder.Configuration);
    builder.Services.AddApiSecurity(builder.Configuration);
    builder.Services.AddTenancy(builder.Configuration);
    builder.Services.AddFeatureFlags(builder.Configuration);
    builder.Services.AddObservability(builder.Configuration);

    // Module system: list module assemblies here; the loader reads each
    // embedded module.json, honors enabled flags and resolves dependency order.
    var modules = ModuleLoader.Load(
        [
            typeof(DemoModule).Assembly,
            typeof(AuthModule).Assembly,
            typeof(AuthorizationModule).Assembly,
            typeof(UsersModule).Assembly,
            typeof(AuditModule).Assembly,
            typeof(SettingsModule).Assembly,
            typeof(EmailModule).Assembly,
            typeof(StorageModule).Assembly,
            typeof(NotificationsModule).Assembly,
            typeof(LocalizationModule).Assembly,
            typeof(RealtimeModule).Assembly,
            typeof(DashboardModule).Assembly,
        ],
        builder.Configuration
    );
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
    app.UseAuthentication();
    // After authentication: the claim strategy needs a principal, and a
    // client-supplied tenant is validated against the caller's own.
    app.UseTenancy();
    app.UseAuthorization();

    // The fallback policy requires authentication everywhere; infrastructure
    // endpoints opt out explicitly.
    app.MapOpenApi().AllowAnonymous();
    app.MapFeatureEndpoints();

    // Liveness = process is up; readiness = external dependencies reachable.
    app.MapHealthChecks("/health/live", new HealthCheckOptions { Predicate = _ => false })
        .AllowAnonymous();
    app.MapHealthChecks(
            "/health/ready",
            new HealthCheckOptions { Predicate = check => check.Tags.Contains("ready") }
        )
        .AllowAnonymous();
    app.MapHealthChecks("/health").AllowAnonymous();

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
