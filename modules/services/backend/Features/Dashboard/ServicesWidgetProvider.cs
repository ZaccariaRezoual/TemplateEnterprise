using System.Globalization;
using EnterpriseFramework.Application.Abstractions;
using EnterpriseFramework.Modules.Authorization.Domain;
using EnterpriseFramework.Modules.Services.Persistence;
using Microsoft.EntityFrameworkCore;

namespace EnterpriseFramework.Modules.Services.Features.Dashboard;

/// <summary>
/// Contributes the catalogue tile to the dashboard.
///
/// The Dashboard module does not know this class exists — it asks the
/// container for providers. This module, in turn, does not know the dashboard
/// renders tiles in a grid. Both sides only share the contract in Application.
/// </summary>
public sealed class ServicesWidgetProvider : IDashboardWidgetProvider
{
    private readonly ServicesDbContext _dbContext;
    private readonly ICurrentUser _currentUser;

    /// <summary>
    /// Initializes the provider.
    /// </summary>
    /// <param name="dbContext">Services persistence.</param>
    /// <param name="currentUser">Caller, for the permission check.</param>
    public ServicesWidgetProvider(ServicesDbContext dbContext, ICurrentUser currentUser)
    {
        _dbContext = dbContext;
        _currentUser = currentUser;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<DashboardWidget>> GetWidgetsAsync(
        CancellationToken cancellationToken = default
    )
    {
        // An empty list, never an exception: a permission gap must hide a
        // tile, not break the page.
        if (!_currentUser.Permissions.Contains(Permissions.Services.Read, StringComparer.Ordinal))
        {
            return [];
        }

        var live = _dbContext.Services.AsNoTracking().Where(service => !service.IsArchived);

        var published = await live.CountAsync(service => service.IsPublished, cancellationToken)
            .ConfigureAwait(false);

        var drafts = await live.CountAsync(service => !service.IsPublished, cancellationToken)
            .ConfigureAwait(false);

        return
        [
            new DashboardWidget(
                "services.published",
                "Services",
                Value: published.ToString(CultureInfo.InvariantCulture),
                // Drafts are the number worth surfacing next to it: a service
                // written and never published is the failure mode of an
                // editorial workflow, and it is invisible on the public site
                // by definition.
                Caption: $"{drafts} draft(s)",
                Link: "/services",
                Order: 20
            ),
        ];
    }
}
