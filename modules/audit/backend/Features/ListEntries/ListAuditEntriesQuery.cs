using EnterpriseFramework.Modules.Audit.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace EnterpriseFramework.Modules.Audit.Features.ListEntries;

/// <summary>
/// Audit entry as exposed to clients.
/// </summary>
/// <param name="Id">Entry identifier.</param>
/// <param name="UserId">Account that acted, or null when anonymous.</param>
/// <param name="Action">Stable action name.</param>
/// <param name="Source">How the entry was produced.</param>
/// <param name="Succeeded">Whether the operation succeeded.</param>
/// <param name="CorrelationId">Correlation id, to join with the logs.</param>
/// <param name="OccurredAtUtc">When the action occurred.</param>
public sealed record AuditEntryDto(
    Guid Id,
    Guid? UserId,
    string Action,
    string Source,
    bool Succeeded,
    string? CorrelationId,
    DateTime OccurredAtUtc
);

/// <summary>
/// Reads the audit trail, newest first.
/// </summary>
/// <param name="Take">Maximum entries to return; capped by the handler.</param>
public sealed record ListAuditEntriesQuery(int Take = 50) : IRequest<IReadOnlyList<AuditEntryDto>>;

/// <summary>
/// Handles <see cref="ListAuditEntriesQuery"/>.
/// </summary>
public sealed class ListAuditEntriesQueryHandler
    : IRequestHandler<ListAuditEntriesQuery, IReadOnlyList<AuditEntryDto>>
{
    private const int MaxTake = 200;

    private readonly AuditDbContext _dbContext;

    /// <summary>
    /// Initializes the handler.
    /// </summary>
    /// <param name="dbContext">Audit persistence.</param>
    public ListAuditEntriesQueryHandler(AuditDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    /// <summary>
    /// Returns the most recent entries.
    /// </summary>
    /// <param name="request">The query.</param>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    /// <returns>The entries, newest first.</returns>
    public async Task<IReadOnlyList<AuditEntryDto>> Handle(
        ListAuditEntriesQuery request,
        CancellationToken cancellationToken
    )
    {
        var take = Math.Clamp(request.Take, 1, MaxTake);

        var entries = await _dbContext
            .Entries.AsNoTracking()
            .OrderByDescending(e => e.OccurredAtUtc)
            .Take(take)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return
        [
            .. entries.Select(e => new AuditEntryDto(
                e.Id,
                e.UserId,
                e.Action,
                e.Source.ToString(),
                e.Succeeded,
                e.CorrelationId,
                e.OccurredAtUtc
            )),
        ];
    }
}
