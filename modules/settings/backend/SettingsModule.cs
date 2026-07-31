using EnterpriseFramework.Modules.Abstractions;
using EnterpriseFramework.Modules.Authorization.Domain;
using EnterpriseFramework.Modules.Settings.Features;
using EnterpriseFramework.Modules.Settings.Persistence;
using EnterpriseFramework.Modules.Settings.Services;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace EnterpriseFramework.Modules.Settings;

/// <summary>
/// Settings module: typed application and per-user settings.
///
/// Other modules read settings through <see cref="SettingsReader"/>, which is
/// registered here; they never query the table, so the layering rules
/// (user → global → declared default) exist in exactly one place.
/// </summary>
public sealed class SettingsModule : IModule
{
    /// <inheritdoc />
    public string Name => "Settings";

    /// <inheritdoc />
    public void ConfigureServices(IServiceCollection services, IConfiguration configuration)
    {
        var connectionString =
            configuration.GetConnectionString("Postgres")
            ?? throw new InvalidOperationException(
                "Connection string 'Postgres' is not configured."
            );

        services.AddDbContext<SettingsDbContext>(options =>
            options.UseNpgsql(
                connectionString,
                npgsql =>
                    npgsql.MigrationsHistoryTable(
                        "__ef_migrations_history",
                        SettingsDbContext.Schema
                    )
            )
        );

        services.AddScoped<SettingsReader>();
        services.AddMediatR(cfg =>
            cfg.RegisterServicesFromAssembly(typeof(SettingsModule).Assembly)
        );

        if (configuration.ShouldAutoMigrate(Name))
        {
            services.AddHostedService<SettingsDbMigrator>();
        }
    }

    /// <inheritdoc />
    public void MapEndpoints(IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/settings").WithTags("Settings");

        group
            .MapGet(
                "/",
                async (ISender sender, CancellationToken ct) =>
                    TypedResults.Ok(await sender.Send(new GetEffectiveSettingsQuery(), ct))
            )
            .WithName("settingsGetEffective")
            .WithSummary("Returns every setting with its effective value for the caller.");

        // Per-user overrides need no permission: users configure themselves.
        group
            .MapPut(
                "/me/{key}",
                async (string key, SetSettingRequest body, ISender sender, CancellationToken ct) =>
                {
                    await sender.Send(new SetSettingCommand(key, body.Value, true), ct);
                    return TypedResults.NoContent();
                }
            )
            .WithName("settingsSetMine")
            .WithSummary("Sets the caller's own value for a setting.");

        // Global values affect everyone, so they are permission-gated.
        group
            .MapPut(
                "/{key}",
                async (string key, SetSettingRequest body, ISender sender, CancellationToken ct) =>
                {
                    await sender.Send(new SetSettingCommand(key, body.Value, false), ct);
                    return TypedResults.NoContent();
                }
            )
            .WithName("settingsSetGlobal")
            .WithSummary("Sets the installation-wide value for a setting.")
            .RequirePermission(Permissions.Settings.Write);
    }
}

/// <summary>
/// Body of the "set setting" endpoints.
/// </summary>
/// <param name="Value">Raw value; must parse as the setting's declared type.</param>
public sealed record SetSettingRequest(string Value);

/// <summary>
/// Applies this module's pending migrations at startup (development only;
/// production runs them as an explicit release step).
/// </summary>
public sealed class SettingsDbMigrator : IHostedService
{
    private readonly IServiceProvider _services;

    /// <summary>
    /// Initializes the migrator.
    /// </summary>
    /// <param name="services">Root provider used to create a migration scope.</param>
    public SettingsDbMigrator(IServiceProvider services)
    {
        _services = services;
    }

    /// <inheritdoc />
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        await using var scope = _services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<SettingsDbContext>();
        await dbContext.Database.MigrateAsync(cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
