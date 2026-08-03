using EnterpriseFramework.Application.Exceptions;
using EnterpriseFramework.Modules.Services.Contracts;
using EnterpriseFramework.Modules.Services.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace EnterpriseFramework.Modules.Services.Features.ListServices;

/// <summary>
/// Lists the whole catalogue for the administration: drafts, published and
/// archived entries alike.
///
/// Unpaged and unfiltered on purpose. A catalogue is a handful of entries by
/// design — the plan deliberately postpones a taxonomy until services no
/// longer fit on one page — so searching and sorting happen on the client,
/// where they cost nothing and answer instantly. The day this list needs a
/// pager is the day it also needs categories, and both arrive together.
/// </summary>
public sealed record ListServicesQuery : IRequest<IReadOnlyList<AdminServiceDto>>;

/// <summary>
/// Handles <see cref="ListServicesQuery"/>.
/// </summary>
public sealed class ListServicesQueryHandler
    : IRequestHandler<ListServicesQuery, IReadOnlyList<AdminServiceDto>>
{
    private readonly ServicesDbContext _dbContext;

    /// <summary>
    /// Initializes the handler.
    /// </summary>
    /// <param name="dbContext">Services persistence.</param>
    public ListServicesQueryHandler(ServicesDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    /// <summary>
    /// Returns the catalogue in showcase order.
    /// </summary>
    /// <param name="request">The query; it carries no parameters.</param>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    /// <returns>Every service, ordered as the showcase would order it.</returns>
    public async Task<IReadOnlyList<AdminServiceDto>> Handle(
        ListServicesQuery request,
        CancellationToken cancellationToken
    )
    {
        var services = await _dbContext
            .Services.AsNoTracking()
            .Include(service => service.Images)
            // Archived last, then showcase order: the administrator reads the
            // live catalogue first, in the order visitors see it.
            .OrderBy(service => service.IsArchived)
            .ThenBy(service => service.SortOrder)
            .ThenBy(service => service.Title)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return [.. services.Select(AdminServiceDto.FromService)];
    }
}

/// <summary>
/// Reads one service of the catalogue, in any state, for the edit form.
/// </summary>
/// <param name="ServiceId">Service to read.</param>
public sealed record GetServiceQuery(Guid ServiceId) : IRequest<AdminServiceDto>;

/// <summary>
/// Handles <see cref="GetServiceQuery"/>.
/// </summary>
public sealed class GetServiceQueryHandler : IRequestHandler<GetServiceQuery, AdminServiceDto>
{
    private readonly ServicesDbContext _dbContext;

    /// <summary>
    /// Initializes the handler.
    /// </summary>
    /// <param name="dbContext">Services persistence.</param>
    public GetServiceQueryHandler(ServicesDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    /// <summary>
    /// Returns the service.
    /// </summary>
    /// <param name="request">The query.</param>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    /// <returns>The service, with its gallery.</returns>
    /// <exception cref="NotFoundException">Thrown when the service does not exist.</exception>
    public async Task<AdminServiceDto> Handle(
        GetServiceQuery request,
        CancellationToken cancellationToken
    )
    {
        var service =
            await _dbContext
                .Services.AsNoTracking()
                .Include(entity => entity.Images)
                .SingleOrDefaultAsync(entity => entity.Id == request.ServiceId, cancellationToken)
                .ConfigureAwait(false) ?? throw new NotFoundException("Service", request.ServiceId);

        return AdminServiceDto.FromService(service);
    }
}
