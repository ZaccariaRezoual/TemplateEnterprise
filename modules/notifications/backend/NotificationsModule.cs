using EnterpriseFramework.Application.Abstractions;
using EnterpriseFramework.Modules.Abstractions;
using EnterpriseFramework.Modules.Notifications.Features;
using EnterpriseFramework.Modules.Notifications.Persistence;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace EnterpriseFramework.Modules.Notifications;

/// <summary>
/// Notifications module: the in-app notification centre.
///
/// Any module reaches a user by publishing <c>NotificationRequested</c> on the
/// event bus — no reference to this module required. Endpoints need no
/// permission because they are inherently scoped to the caller: a user may
/// always read and dismiss their own notifications.
/// </summary>
public sealed class NotificationsModule : IModule
{
    /// <inheritdoc />
    public string Name => "Notifications";

    /// <inheritdoc />
    public void ConfigureServices(IServiceCollection services, IConfiguration configuration)
    {
        var connectionString =
            configuration.GetConnectionString("Postgres")
            ?? throw new InvalidOperationException(
                "Connection string 'Postgres' is not configured."
            );

        services.AddDbContext<NotificationsDbContext>(options =>
            options.UseNpgsql(
                connectionString,
                npgsql =>
                    npgsql.MigrationsHistoryTable(
                        "__ef_migrations_history",
                        NotificationsDbContext.Schema
                    )
            )
        );

        services.AddScoped<IDashboardWidgetProvider, Features.Dashboard.NotificationsWidgetProvider>();

        services.AddMediatR(cfg =>
            cfg.RegisterServicesFromAssembly(typeof(NotificationsModule).Assembly)
        );

        if (configuration.ShouldAutoMigrate(Name))
        {
            services.AddHostedService<NotificationsDbMigrator>();
        }
    }

    /// <inheritdoc />
    public void MapEndpoints(IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/notifications").WithTags("Notifications");

        group
            .MapGet(
                "/",
                async (ISender sender, CancellationToken ct, bool unreadOnly = false, int take = 50) =>
                    TypedResults.Ok(await sender.Send(new ListNotificationsQuery(unreadOnly, take), ct))
            )
            .WithName("notificationsList")
            .WithSummary("Lists the caller's notifications, newest first.");

        group
            .MapPost(
                "/read",
                async (MarkReadRequest body, ISender sender, CancellationToken ct) =>
                {
                    await sender.Send(new MarkNotificationsReadCommand(body.NotificationId), ct);
                    return TypedResults.NoContent();
                }
            )
            .WithName("notificationsMarkRead")
            .WithSummary("Marks one notification, or all of them, as read.");
    }
}

/// <summary>
/// Body of the "mark read" endpoint.
/// </summary>
/// <param name="NotificationId">The notification to mark, or null for all.</param>
public sealed record MarkReadRequest(Guid? NotificationId);

/// <summary>
/// Applies this module's pending migrations at startup (development only;
/// production runs them as an explicit release step).
/// </summary>
public sealed class NotificationsDbMigrator : IHostedService
{
    private readonly IServiceProvider _services;

    /// <summary>
    /// Initializes the migrator.
    /// </summary>
    /// <param name="services">Root provider used to create a migration scope.</param>
    public NotificationsDbMigrator(IServiceProvider services)
    {
        _services = services;
    }

    /// <inheritdoc />
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        await using var scope = _services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<NotificationsDbContext>();
        await dbContext.Database.MigrateAsync(cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
