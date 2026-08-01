using System.Globalization;
using EnterpriseFramework.Application.Abstractions;
using EnterpriseFramework.Modules.Notifications.Persistence;
using Microsoft.EntityFrameworkCore;

namespace EnterpriseFramework.Modules.Notifications.Features.Dashboard;

/// <summary>
/// Contributes the caller's unread-notification tile.
///
/// Needs no permission check: the data is inherently the caller's own, and
/// the query is filtered by their id — the same reasoning that leaves the
/// notification endpoints ungated.
/// </summary>
public sealed class NotificationsWidgetProvider : IDashboardWidgetProvider
{
    private readonly NotificationsDbContext _dbContext;
    private readonly ICurrentUser _currentUser;

    /// <summary>
    /// Initializes the provider.
    /// </summary>
    /// <param name="dbContext">Notifications persistence.</param>
    /// <param name="currentUser">Caller whose notifications are counted.</param>
    public NotificationsWidgetProvider(
        NotificationsDbContext dbContext,
        ICurrentUser currentUser
    )
    {
        _dbContext = dbContext;
        _currentUser = currentUser;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<DashboardWidget>> GetWidgetsAsync(
        CancellationToken cancellationToken = default
    )
    {
        if (_currentUser.UserId is not { } userId)
        {
            return [];
        }

        var unread = await _dbContext
            .Notifications.AsNoTracking()
            .CountAsync(
                notification => notification.UserId == userId && notification.ReadAtUtc == null,
                cancellationToken
            )
            .ConfigureAwait(false);

        return
        [
            new DashboardWidget(
                "notifications.unread",
                "Unread",
                Value: unread.ToString(CultureInfo.InvariantCulture),
                Caption: unread == 0 ? "You are up to date" : "notifications waiting",
                Order: 20
            ),
        ];
    }
}
