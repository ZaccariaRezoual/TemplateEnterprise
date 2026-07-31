using EnterpriseFramework.Modules.Abstractions;
using EnterpriseFramework.Modules.Audit.Behaviors;
using EnterpriseFramework.Modules.Audit.Features.ListEntries;
using EnterpriseFramework.Modules.Audit.Persistence;
using EnterpriseFramework.Modules.Authorization.Domain;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace EnterpriseFramework.Modules.Audit;

/// <summary>
/// Audit module: records who did what, when.
///
/// Demonstrates the two ways a module can observe the rest of the system
/// without being referenced by it: a PIPELINE BEHAVIOR that sees every
/// command, and EVENT SUBSCRIBERS on other modules' public contracts.
/// Installing it adds an audit trail to modules that were written before it
/// existed.
/// </summary>
public sealed class AuditModule : IModule
{
    /// <inheritdoc />
    public string Name => "Audit";

    /// <inheritdoc />
    public void ConfigureServices(IServiceCollection services, IConfiguration configuration)
    {
        var connectionString =
            configuration.GetConnectionString("Postgres")
            ?? throw new InvalidOperationException(
                "Connection string 'Postgres' is not configured."
            );

        services.AddDbContext<AuditDbContext>(options =>
            options.UseNpgsql(
                connectionString,
                npgsql =>
                    npgsql.MigrationsHistoryTable("__ef_migrations_history", AuditDbContext.Schema)
            )
        );

        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(AuditModule).Assembly));

        // Registered last so it wraps the whole pipeline: the audit entry
        // reflects the final outcome, including validation failures.
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(AuditBehavior<,>));

        if (configuration.ShouldAutoMigrate(Name))
        {
            services.AddHostedService<AuditDbMigrator>();
        }
    }

    /// <inheritdoc />
    public void MapEndpoints(IEndpointRouteBuilder endpoints)
    {
        endpoints
            .MapGet(
                "/api/audit",
                async (ISender sender, CancellationToken ct, int take = 50) =>
                    TypedResults.Ok(await sender.Send(new ListAuditEntriesQuery(take), ct))
            )
            .WithTags("Audit")
            .WithName("auditList")
            .WithSummary("Reads the audit trail, newest first.")
            .RequirePermission(Permissions.Audit.Read);
    }
}

/// <summary>
/// Applies this module's pending migrations at startup (development default;
/// production runs them as an explicit release step).
/// </summary>
public sealed class AuditDbMigrator : IHostedService
{
    private readonly IServiceProvider _services;

    /// <summary>
    /// Initializes the migrator.
    /// </summary>
    /// <param name="services">Root provider used to create a migration scope.</param>
    public AuditDbMigrator(IServiceProvider services)
    {
        _services = services;
    }

    /// <inheritdoc />
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        await using var scope = _services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AuditDbContext>();
        await dbContext.Database.MigrateAsync(cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
