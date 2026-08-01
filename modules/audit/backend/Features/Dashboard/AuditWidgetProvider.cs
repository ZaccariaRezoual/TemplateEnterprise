using EnterpriseFramework.Application.Abstractions;
using EnterpriseFramework.Modules.Audit.Persistence;
using EnterpriseFramework.Modules.Authorization.Domain;
using Microsoft.EntityFrameworkCore;

namespace EnterpriseFramework.Modules.Audit.Features.Dashboard;

/// <summary>
/// Contributes a "recent activity" list tile, for callers allowed to read the
/// audit trail.
/// </summary>
public sealed class AuditWidgetProvider : IDashboardWidgetProvider
{
    private const int RecentCount = 5;

    private readonly AuditDbContext _dbContext;
    private readonly ICurrentUser _currentUser;

    /// <summary>
    /// Initializes the provider.
    /// </summary>
    /// <param name="dbContext">Audit persistence.</param>
    /// <param name="currentUser">Caller, for the permission check.</param>
    public AuditWidgetProvider(AuditDbContext dbContext, ICurrentUser currentUser)
    {
        _dbContext = dbContext;
        _currentUser = currentUser;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<DashboardWidget>> GetWidgetsAsync(
        CancellationToken cancellationToken = default
    )
    {
        if (!_currentUser.Permissions.Contains(Permissions.Audit.Read, StringComparer.Ordinal))
        {
            return [];
        }

        var recent = await _dbContext
            .Entries.AsNoTracking()
            .OrderByDescending(entry => entry.OccurredAtUtc)
            .Take(RecentCount)
            .Select(entry => new { entry.Action, entry.OccurredAtUtc, entry.Succeeded })
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return
        [
            new DashboardWidget(
                "audit.recent",
                "Recent activity",
                DashboardWidgetKind.List,
                Items:
                [
                    .. recent.Select(entry => new DashboardWidgetItem(
                        entry.Action,
                        // The outcome matters more than the exact time here:
                        // a failed action is the one worth noticing.
                        entry.Succeeded ? entry.OccurredAtUtc.ToString("O") : "failed"
                    )),
                ],
                Order: 40
            ),
        ];
    }
}
