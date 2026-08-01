using System.Globalization;
using EnterpriseFramework.Application.Abstractions;
using EnterpriseFramework.Modules.Authorization.Domain;
using EnterpriseFramework.Modules.Users.Persistence;
using Microsoft.EntityFrameworkCore;

namespace EnterpriseFramework.Modules.Users.Features.Dashboard;

/// <summary>
/// Contributes the account tiles to the dashboard.
///
/// The Dashboard module does not know this class exists — it asks the
/// container for providers. This module, in turn, does not know the dashboard
/// renders tiles in a grid. Both sides only share the contract in Application.
/// </summary>
public sealed class UsersWidgetProvider : IDashboardWidgetProvider
{
    private readonly UsersDbContext _dbContext;
    private readonly ICurrentUser _currentUser;

    /// <summary>
    /// Initializes the provider.
    /// </summary>
    /// <param name="dbContext">Users persistence (already tenant-scoped).</param>
    /// <param name="currentUser">Caller, for the permission check.</param>
    public UsersWidgetProvider(UsersDbContext dbContext, ICurrentUser currentUser)
    {
        _dbContext = dbContext;
        _currentUser = currentUser;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<DashboardWidget>> GetWidgetsAsync(
        CancellationToken cancellationToken = default
    )
    {
        // The provider decides what the caller may see. The dashboard cannot
        // judge a tile's contents, so filtering here is the only place it can
        // correctly happen. An empty list, never an exception: a permission
        // gap must hide a tile, not break the page.
        if (!_currentUser.Permissions.Contains(Permissions.Users.Read, StringComparer.Ordinal))
        {
            return [];
        }

        var total = await _dbContext
            .Profiles.AsNoTracking()
            .CountAsync(cancellationToken)
            .ConfigureAwait(false);

        var active = await _dbContext
            .Profiles.AsNoTracking()
            .CountAsync(profile => profile.IsActive, cancellationToken)
            .ConfigureAwait(false);

        return
        [
            new DashboardWidget(
                "users.total",
                "Users",
                Value: total.ToString(CultureInfo.InvariantCulture),
                Caption: $"{active} active",
                Link: "/users",
                Order: 10
            ),
        ];
    }
}
