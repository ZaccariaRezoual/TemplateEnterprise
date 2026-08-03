using EnterpriseFramework.Application.Exceptions;
using EnterpriseFramework.Modules.Services.Contracts;
using EnterpriseFramework.Modules.Services.Domain;
using EnterpriseFramework.Modules.Services.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace EnterpriseFramework.Modules.Services.Features.PublicCatalogue;

/// <summary>
/// The single predicate that decides what the public may see.
///
/// It exists as one expression, in one place, because the failure this module
/// must make impossible is a draft reaching the showcase. Two endpoints
/// writing "IsPublished &amp;&amp; !IsArchived" by hand is two chances to write it
/// wrong, and the wrong version does not fail — it publishes.
/// </summary>
internal static class PublicCatalogue
{
    /// <summary>
    /// Restricts a query to the services an anonymous visitor may see.
    /// </summary>
    /// <param name="services">The catalogue query to restrict.</param>
    /// <returns>The query, filtered and in showcase order.</returns>
    public static IQueryable<Service> Visible(IQueryable<Service> services) =>
        services
            .Where(service => service.IsPublished && !service.IsArchived)
            .OrderBy(service => service.SortOrder)
            .ThenBy(service => service.Title);
}

/// <summary>
/// Lists the services the showcase displays.
/// </summary>
public sealed record ListPublicServicesQuery : IRequest<IReadOnlyList<PublicServiceDto>>;

/// <summary>
/// Handles <see cref="ListPublicServicesQuery"/>.
/// </summary>
public sealed class ListPublicServicesQueryHandler
    : IRequestHandler<ListPublicServicesQuery, IReadOnlyList<PublicServiceDto>>
{
    private readonly ServicesDbContext _dbContext;

    /// <summary>
    /// Initializes the handler.
    /// </summary>
    /// <param name="dbContext">Services persistence.</param>
    public ListPublicServicesQueryHandler(ServicesDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    /// <summary>
    /// Returns the published catalogue.
    /// </summary>
    /// <param name="request">The query; it carries no parameters.</param>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    /// <returns>The published, non-archived services in showcase order.</returns>
    public async Task<IReadOnlyList<PublicServiceDto>> Handle(
        ListPublicServicesQuery request,
        CancellationToken cancellationToken
    )
    {
        var services = await PublicCatalogue
            .Visible(_dbContext.Services.AsNoTracking().Include(service => service.Images))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return [.. services.Select(PublicServiceDto.FromService)];
    }
}

/// <summary>
/// Reads one service of the showcase by its address.
/// </summary>
/// <param name="Slug">URL segment of the page being opened.</param>
public sealed record GetPublicServiceQuery(string Slug) : IRequest<PublicServiceDto>;

/// <summary>
/// Handles <see cref="GetPublicServiceQuery"/>.
/// </summary>
public sealed partial class GetPublicServiceQueryHandler
    : IRequestHandler<GetPublicServiceQuery, PublicServiceDto>
{
    private readonly ServicesDbContext _dbContext;
    private readonly ILogger<GetPublicServiceQueryHandler> _logger;

    [LoggerMessage(
        Level = LogLevel.Debug,
        Message = "Public service page requested for an address that is not visible"
    )]
    private static partial void LogNotVisible(ILogger logger);

    /// <summary>
    /// Initializes the handler.
    /// </summary>
    /// <param name="dbContext">Services persistence.</param>
    /// <param name="logger">Logger used to report misses.</param>
    public GetPublicServiceQueryHandler(
        ServicesDbContext dbContext,
        ILogger<GetPublicServiceQueryHandler> logger
    )
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    /// <summary>
    /// Returns the service behind a public address.
    /// </summary>
    /// <param name="request">The query.</param>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    /// <returns>The service.</returns>
    /// <exception cref="NotFoundException">
    /// Thrown when no VISIBLE service carries that address. A draft answers
    /// exactly like a slug that never existed: telling the two apart would let
    /// anyone enumerate work in progress.
    /// </exception>
    public async Task<PublicServiceDto> Handle(
        GetPublicServiceQuery request,
        CancellationToken cancellationToken
    )
    {
        var service = await PublicCatalogue
            .Visible(_dbContext.Services.AsNoTracking().Include(entity => entity.Images))
            .SingleOrDefaultAsync(entity => entity.Slug == request.Slug, cancellationToken)
            .ConfigureAwait(false);

        if (service is null)
        {
            // Without the slug: a public 404 is routinely triggered by
            // crawlers, and logging arbitrary path segments is how log
            // injection gets in.
            LogNotVisible(_logger);
            throw new NotFoundException("Service", request.Slug);
        }

        return PublicServiceDto.FromService(service);
    }
}
