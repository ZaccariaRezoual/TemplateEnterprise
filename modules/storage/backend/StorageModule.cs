using EnterpriseFramework.Application.Abstractions;
using EnterpriseFramework.Application.Exceptions;
using EnterpriseFramework.Modules.Abstractions;
using EnterpriseFramework.Modules.Authorization.Domain;
using EnterpriseFramework.Modules.Storage.Abstractions;
using EnterpriseFramework.Modules.Storage.Domain;
using EnterpriseFramework.Modules.Storage.Options;
using EnterpriseFramework.Modules.Storage.Persistence;
using EnterpriseFramework.Modules.Storage.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace EnterpriseFramework.Modules.Storage;

/// <summary>
/// Storage module: upload, download and delete files.
///
/// Bytes go to an <see cref="IFileStorageProvider"/> (local disk by default,
/// object storage in production); metadata stays in this module's schema.
/// Endpoints are written directly rather than through MediatR because they
/// stream: buffering a file into a command object would defeat the point.
/// </summary>
public sealed class StorageModule : IModule
{
    /// <inheritdoc />
    public string Name => "Storage";

    /// <inheritdoc />
    public void ConfigureServices(IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<StorageOptions>(configuration.GetSection(StorageOptions.SectionName));

        var connectionString =
            configuration.GetConnectionString("Postgres")
            ?? throw new InvalidOperationException(
                "Connection string 'Postgres' is not configured."
            );

        services.AddDbContext<StorageDbContext>(options =>
            options.UseNpgsql(
                connectionString,
                npgsql =>
                    npgsql.MigrationsHistoryTable("__ef_migrations_history", StorageDbContext.Schema)
            )
        );

        services.AddSingleton<IFileStorageProvider, LocalFileStorageProvider>();

        if (configuration.ShouldAutoMigrate(Name))
        {
            services.AddHostedService<StorageDbMigrator>();
        }
    }

    /// <inheritdoc />
    public void MapEndpoints(IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/files").WithTags("Files");

        group
            .MapPost("/", UploadAsync)
            .WithName("filesUpload")
            .WithSummary("Uploads a file and returns its metadata.")
            .RequirePermission(Permissions.Files.Write)
            .DisableAntiforgery();

        group
            .MapGet("/{id:guid}", DownloadAsync)
            .WithName("filesDownload")
            .WithSummary("Downloads a stored file.");

        group
            .MapDelete("/{id:guid}", DeleteAsync)
            .WithName("filesDelete")
            .WithSummary("Deletes a stored file and its bytes.")
            .RequirePermission(Permissions.Files.Delete);
    }

    // Concrete result types, not IResult: they carry the response shape into
    // the OpenAPI document and therefore into the generated SDK.
    private static async Task<Ok<StoredFileDto>> UploadAsync(
        IFormFile file,
        StorageDbContext dbContext,
        IFileStorageProvider provider,
        ICurrentUser currentUser,
        IOptions<StorageOptions> options,
        CancellationToken cancellationToken
    )
    {
        if (file.Length == 0)
        {
            throw new BusinessException("The uploaded file is empty.");
        }

        if (file.Length > options.Value.MaxUploadBytes)
        {
            throw new BusinessException(
                $"The file exceeds the maximum size of {options.Value.MaxUploadBytes} bytes."
            );
        }

        var userId =
            currentUser.UserId ?? throw new UnauthorizedException("Authentication is required.");

        await using var stream = file.OpenReadStream();
        var storageKey = await provider.SaveAsync(stream, cancellationToken);

        // The client-declared content type is stored but NEVER trusted on
        // download (see DownloadAsync): a "image/png" claim on an HTML payload
        // is how stored-XSS gets served from your own origin.
        var stored = StoredFile.Record(
            Path.GetFileName(file.FileName),
            file.ContentType,
            file.Length,
            storageKey,
            userId
        );

        dbContext.Files.Add(stored);
        await dbContext.SaveChangesAsync(cancellationToken);

        return TypedResults.Ok(
            new StoredFileDto(
                stored.Id,
                stored.FileName,
                stored.ContentType,
                stored.SizeInBytes,
                stored.UploadedAtUtc
            )
        );
    }

    private static async Task<FileStreamHttpResult> DownloadAsync(
        Guid id,
        StorageDbContext dbContext,
        IFileStorageProvider provider,
        CancellationToken cancellationToken
    )
    {
        var stored =
            await dbContext.Files.AsNoTracking().SingleOrDefaultAsync(f => f.Id == id, cancellationToken)
            ?? throw new NotFoundException("File", id);

        var content = await provider.OpenAsync(stored.StorageKey, cancellationToken);

        // Served as a download with a generic content type: never echo the
        // uploader's content type, or an uploaded .html executes on this
        // origin with the user's session.
        return TypedResults.File(
            content,
            "application/octet-stream",
            stored.FileName,
            enableRangeProcessing: true
        );
    }

    private static async Task<NoContent> DeleteAsync(
        Guid id,
        StorageDbContext dbContext,
        IFileStorageProvider provider,
        CancellationToken cancellationToken
    )
    {
        var stored = await dbContext.Files.SingleOrDefaultAsync(f => f.Id == id, cancellationToken);
        if (stored is null)
        {
            // Idempotent: the caller's goal (the file is gone) already holds.
            return TypedResults.NoContent();
        }

        // Metadata first: an orphaned blob is recoverable garbage, while a
        // metadata row pointing at deleted bytes is a broken download.
        dbContext.Files.Remove(stored);
        await dbContext.SaveChangesAsync(cancellationToken);
        await provider.DeleteAsync(stored.StorageKey, cancellationToken);

        return TypedResults.NoContent();
    }
}

/// <summary>
/// File metadata as exposed to clients. The storage key is never included:
/// it is an internal detail of the provider.
/// </summary>
/// <param name="Id">Public identifier used to download or delete.</param>
/// <param name="FileName">Name as uploaded.</param>
/// <param name="ContentType">Content type declared at upload.</param>
/// <param name="SizeInBytes">Size in bytes.</param>
/// <param name="UploadedAtUtc">When it was uploaded.</param>
public sealed record StoredFileDto(
    Guid Id,
    string FileName,
    string ContentType,
    long SizeInBytes,
    DateTime UploadedAtUtc
);

/// <summary>
/// Applies this module's pending migrations at startup (development only;
/// production runs them as an explicit release step).
/// </summary>
public sealed class StorageDbMigrator : IHostedService
{
    private readonly IServiceProvider _services;

    /// <summary>
    /// Initializes the migrator.
    /// </summary>
    /// <param name="services">Root provider used to create a migration scope.</param>
    public StorageDbMigrator(IServiceProvider services)
    {
        _services = services;
    }

    /// <inheritdoc />
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        await using var scope = _services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<StorageDbContext>();
        await dbContext.Database.MigrateAsync(cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
