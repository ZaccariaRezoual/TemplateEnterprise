using EnterpriseFramework.Modules.Abstractions;
using EnterpriseFramework.Modules.Authorization.Domain;
using EnterpriseFramework.Modules.Services.Features;
using EnterpriseFramework.Modules.Services.Features.ArchiveService;
using EnterpriseFramework.Modules.Services.Features.CreateService;
using EnterpriseFramework.Modules.Services.Features.ListServices;
using EnterpriseFramework.Modules.Services.Features.PublicCatalogue;
using EnterpriseFramework.Modules.Services.Features.ServiceImages;
using EnterpriseFramework.Modules.Services.Features.UpdateService;
using EnterpriseFramework.Modules.Services.Persistence;
using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace EnterpriseFramework.Modules.Services;

/// <summary>
/// Services module: the catalogue of what the organization offers.
///
/// Responsibilities: administering services (create, edit, publish, archive,
/// illustrate) and serving them to the public showcase. It owns both, because
/// whoever owns the data owns its representation — the Site module stays the
/// shell and the institutional pages.
///
/// The endpoints come in two families that never meet: <c>/api/services</c> is
/// anonymous and can only ever return published entries, <c>/api/admin/services</c>
/// requires a permission and sees everything. They are two routes rather than
/// one route with a flag because a flag is something a caller can set and a
/// reviewer can miss — and a draft on the public site is the one failure this
/// module exists to prevent.
///
/// It does not know the Storage module: an image is a file identifier and an
/// alt text, and the browser resolves the identifier against Storage's own
/// public endpoint.
/// </summary>
public sealed class ServicesModule : IModule
{
    /// <inheritdoc />
    public string Name => "Services";

    /// <inheritdoc />
    public void ConfigureServices(IServiceCollection services, IConfiguration configuration)
    {
        var connectionString =
            configuration.GetConnectionString("Postgres")
            ?? throw new InvalidOperationException(
                "Connection string 'Postgres' is not configured."
            );

        services.AddDbContext<ServicesDbContext>(options =>
            options.UseNpgsql(
                connectionString,
                npgsql =>
                    npgsql.MigrationsHistoryTable(
                        "__ef_migrations_history",
                        ServicesDbContext.Schema
                    )
            )
        );

        services.AddMediatR(cfg =>
            cfg.RegisterServicesFromAssembly(typeof(ServicesModule).Assembly)
        );
        services.AddValidatorsFromAssembly(typeof(ServicesModule).Assembly);

        // Contributes the "published services" tile. The Dashboard module
        // never learns this module exists; it resolves providers from the
        // container.
        services.AddScoped<
            Application.Abstractions.IDashboardWidgetProvider,
            Features.Dashboard.ServicesWidgetProvider
        >();

        if (configuration.ShouldAutoMigrate(Name))
        {
            services.AddHostedService<ServicesDbMigrator>();
        }
    }

    /// <inheritdoc />
    public void MapEndpoints(IEndpointRouteBuilder endpoints)
    {
        MapPublicEndpoints(endpoints);
        MapAdminEndpoints(endpoints);
    }

    /// <summary>
    /// The showcase API: anonymous, read-only, published entries only.
    /// </summary>
    private static void MapPublicEndpoints(IEndpointRouteBuilder endpoints)
    {
        var group = endpoints
            .MapGroup("/api/services")
            .WithTags("Services")
            // Anonymous by necessity: this is what the public site reads, and
            // a visitor has no account. Safe to expose because the queries
            // behind it cannot return anything but published, non-archived
            // entries — see PublicCatalogue.Visible.
            .AllowAnonymous();

        group
            .MapGet(
                "/",
                async (ISender sender, CancellationToken ct) =>
                    TypedResults.Ok(await sender.Send(new ListPublicServicesQuery(), ct))
            )
            .WithName("servicesList")
            .WithSummary("Lists the published services, in showcase order.");

        group
            .MapGet(
                "/{slug}",
                async (string slug, ISender sender, CancellationToken ct) =>
                    TypedResults.Ok(await sender.Send(new GetPublicServiceQuery(slug), ct))
            )
            .WithName("servicesGetBySlug")
            .WithSummary("Reads one published service by its public address.");
    }

    /// <summary>
    /// The administration API: every entry, behind the services permissions.
    /// </summary>
    private static void MapAdminEndpoints(IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/admin/services").WithTags("Services administration");

        group
            .MapGet(
                "/",
                async (ISender sender, CancellationToken ct) =>
                    TypedResults.Ok(await sender.Send(new ListServicesQuery(), ct))
            )
            .WithName("adminServicesList")
            .WithSummary("Lists the whole catalogue: drafts, published and archived.")
            .RequirePermission(Permissions.Services.Read);

        group
            .MapGet(
                "/{id:guid}",
                async (Guid id, ISender sender, CancellationToken ct) =>
                    TypedResults.Ok(await sender.Send(new GetServiceQuery(id), ct))
            )
            .WithName("adminServicesGet")
            .WithSummary("Reads one service in any state, for the edit form.")
            .RequirePermission(Permissions.Services.Read);

        group
            .MapPost(
                "/",
                async (ServiceWriteModel body, ISender sender, CancellationToken ct) =>
                    TypedResults.Ok(await sender.Send(new CreateServiceCommand(body), ct))
            )
            .WithName("adminServicesCreate")
            .WithSummary("Adds a service to the catalogue.")
            .RequirePermission(Permissions.Services.Write)
            .ProducesValidationProblem();

        group
            .MapPut(
                "/{id:guid}",
                async (Guid id, ServiceWriteModel body, ISender sender, CancellationToken ct) =>
                    TypedResults.Ok(await sender.Send(new UpdateServiceCommand(id, body), ct))
            )
            .WithName("adminServicesUpdate")
            .WithSummary("Edits a service of the catalogue.")
            .RequirePermission(Permissions.Services.Write)
            .ProducesValidationProblem();

        group
            .MapPost(
                "/{id:guid}/images",
                async (Guid id, AttachImageRequest body, ISender sender, CancellationToken ct) =>
                    TypedResults.Ok(
                        await sender.Send(
                            new AddServiceImageCommand(
                                id,
                                body.StorageFileId,
                                body.AltText,
                                body.SortOrder
                            ),
                            ct
                        )
                    )
            )
            .WithName("adminServicesAddImage")
            .WithSummary("Attaches an uploaded file to a service as one of its images.")
            .RequirePermission(Permissions.Services.Write)
            .ProducesValidationProblem();

        group
            .MapPut(
                "/{id:guid}/images/{imageId:guid}/cover",
                async (Guid id, Guid imageId, ISender sender, CancellationToken ct) =>
                {
                    await sender.Send(new SetServiceCoverCommand(id, imageId), ct);
                    return TypedResults.NoContent();
                }
            )
            .WithName("adminServicesSetCover")
            .WithSummary("Chooses the image shown on cards and in link previews.")
            .RequirePermission(Permissions.Services.Write);

        group
            .MapDelete(
                "/{id:guid}/images/{imageId:guid}",
                async (Guid id, Guid imageId, ISender sender, CancellationToken ct) =>
                {
                    await sender.Send(new RemoveServiceImageCommand(id, imageId), ct);
                    return TypedResults.NoContent();
                }
            )
            .WithName("adminServicesRemoveImage")
            .WithSummary("Detaches an image from a service; the file itself is left to Storage.")
            .RequirePermission(Permissions.Services.Write);

        group
            .MapPost(
                "/{id:guid}/archive",
                async (Guid id, ISender sender, CancellationToken ct) =>
                {
                    await sender.Send(new ArchiveServiceCommand(id), ct);
                    return TypedResults.NoContent();
                }
            )
            .WithName("adminServicesArchive")
            .WithSummary("Withdraws a service from the catalogue. There is no delete.")
            // Its own permission: withdrawing a service changes what the
            // public sees and what can still be booked, which is a heavier
            // act than editing a description.
            .RequirePermission(Permissions.Services.Delete);
    }
}

/// <summary>
/// Body of the "attach image" endpoint.
/// </summary>
/// <param name="StorageFileId">
/// Identifier returned by <c>POST /api/files</c>. Upload it as a PUBLIC file:
/// a private one cannot be shown to an anonymous visitor.
/// </param>
/// <param name="AltText">
/// What the image shows, for anyone who cannot see it. Required.
/// </param>
/// <param name="SortOrder">Position in the gallery; lower comes first.</param>
public sealed record AttachImageRequest(Guid StorageFileId, string AltText, int SortOrder);

/// <summary>
/// Applies this module's pending migrations at startup (development default;
/// production runs them as an explicit release step).
/// </summary>
public sealed class ServicesDbMigrator : IHostedService
{
    private readonly IServiceProvider _services;

    /// <summary>
    /// Initializes the migrator.
    /// </summary>
    /// <param name="services">Root provider used to create a migration scope.</param>
    public ServicesDbMigrator(IServiceProvider services)
    {
        _services = services;
    }

    /// <inheritdoc />
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        await using var scope = _services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ServicesDbContext>();
        await dbContext.Database.MigrateAsync(cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
