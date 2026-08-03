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
using Microsoft.AspNetCore.Mvc;
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
            .MapGet("/public/{id:guid}", DownloadPublicAsync)
            .WithName("filesDownloadPublic")
            .WithSummary("Downloads a file that was uploaded as public. No token required.")
            // Anonymous by necessity: a showcase page has images, and the
            // visitor looking at it has no account. It is a SEPARATE endpoint
            // rather than a check inside the authenticated one because the
            // rule "this route can only ever serve public files" is then a
            // property of the route, not of a branch someone can edit.
            .AllowAnonymous();

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
        CancellationToken cancellationToken,
        // Private unless the caller says otherwise, and the parameter is read
        // from the form so making a file public is always something someone
        // wrote down. A default of "public" would turn a forgotten field into
        // a disclosure.
        [FromForm] FileVisibility visibility = FileVisibility.Private
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

        // The bytes decide what this file is. The client-declared content
        // type is kept as metadata and never served: an "image/png" claim on
        // an HTML payload is how stored XSS gets served from your own origin.
        byte[] header;
        await using (var probe = file.OpenReadStream())
        {
            header = await ContentSniffer.ReadHeaderAsync(probe, cancellationToken);
        }

        // A second stream rather than a rewind: nothing in IFormFile promises
        // the first one is seekable, and a provider handed a half-read stream
        // stores a truncated file without failing.
        await using var stream = file.OpenReadStream();
        var storageKey = await provider.SaveAsync(stream, cancellationToken);
        var safeContentType = ContentSniffer.Detect(header);

        var stored = StoredFile.Record(
            Path.GetFileName(file.FileName),
            file.ContentType,
            safeContentType,
            file.Length,
            storageKey,
            userId,
            visibility
        );

        dbContext.Files.Add(stored);
        await dbContext.SaveChangesAsync(cancellationToken);

        return TypedResults.Ok(StoredFileDto.FromFile(stored));
    }

    private static async Task<FileStreamHttpResult> DownloadAsync(
        Guid id,
        HttpContext httpContext,
        StorageDbContext dbContext,
        IFileStorageProvider provider,
        CancellationToken cancellationToken
    )
    {
        var stored =
            await dbContext.Files.AsNoTracking().SingleOrDefaultAsync(f => f.Id == id, cancellationToken)
            ?? throw new NotFoundException("File", id);

        return await ServeAsync(stored, httpContext, provider, cancellationToken);
    }

    private static async Task<FileStreamHttpResult> DownloadPublicAsync(
        Guid id,
        HttpContext httpContext,
        StorageDbContext dbContext,
        IFileStorageProvider provider,
        CancellationToken cancellationToken
    )
    {
        // The visibility is part of the LOOKUP, not a check after it: a query
        // that cannot return a private file is a guarantee, while an `if`
        // after the load is a line someone can move.
        var stored =
            await dbContext
                .Files.AsNoTracking()
                .SingleOrDefaultAsync(
                    f => f.Id == id && f.Visibility == FileVisibility.Public,
                    cancellationToken
                )
            // 404 rather than 403: a private file must not be distinguishable
            // from one that does not exist, or the endpoint becomes a way to
            // test whether an identifier is in use.
            ?? throw new NotFoundException("File", id);

        return await ServeAsync(stored, httpContext, provider, cancellationToken);
    }

    /// <summary>
    /// Streams a file with the content type derived from its own bytes.
    ///
    /// A recognized image is served inline so a page can display it;
    /// everything else is served as an opaque attachment, which no browser
    /// executes. <c>nosniff</c> closes the remaining gap: without it a browser
    /// may decide for itself that our octet-stream is really HTML.
    /// </summary>
    private static async Task<FileStreamHttpResult> ServeAsync(
        StoredFile stored,
        HttpContext httpContext,
        IFileStorageProvider provider,
        CancellationToken cancellationToken
    )
    {
        var content = await provider.OpenAsync(stored.StorageKey, cancellationToken);

        httpContext.Response.Headers.XContentTypeOptions = "nosniff";

        var isRenderable = stored.SafeContentType != ContentSniffer.OpaqueContentType;

        return TypedResults.File(
            content,
            stored.SafeContentType,
            // A download name is what turns the response into an attachment.
            // An image must not get one, or the browser saves it instead of
            // painting it into the page.
            isRenderable ? null : stored.FileName,
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
/// <param name="ContentType">Content type declared at upload; metadata only.</param>
/// <param name="SizeInBytes">Size in bytes.</param>
/// <param name="Visibility">
/// Who may download it. A <c>Public</c> file is reachable without a token at
/// <c>/api/files/public/{id}</c> — that is the URL to put in an
/// <c>&lt;img src&gt;</c>.
/// </param>
/// <param name="UploadedAtUtc">When it was uploaded.</param>
public sealed record StoredFileDto(
    Guid Id,
    string FileName,
    string ContentType,
    long SizeInBytes,
    FileVisibility Visibility,
    DateTime UploadedAtUtc
)
{
    /// <summary>
    /// Maps a stored file to its client representation.
    /// </summary>
    /// <param name="file">The metadata row.</param>
    /// <returns>The DTO.</returns>
    public static StoredFileDto FromFile(StoredFile file) =>
        new(
            file.Id,
            file.FileName,
            file.ContentType,
            file.SizeInBytes,
            file.Visibility,
            file.UploadedAtUtc
        );
}

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
