namespace EnterpriseFramework.Application.Abstractions;

/// <summary>Shape a widget takes on screen.</summary>
public enum DashboardWidgetKind
{
    /// <summary>A single number with an optional trend — the default.</summary>
    Stat = 0,

    /// <summary>A short list of recent items.</summary>
    List = 1,
}

/// <summary>One entry of a <see cref="DashboardWidgetKind.List"/> widget.</summary>
/// <param name="Label">Primary text.</param>
/// <param name="Detail">Secondary text, e.g. a timestamp or actor.</param>
public sealed record DashboardWidgetItem(string Label, string? Detail = null);

/// <summary>
/// A tile contributed to the dashboard by a module.
/// </summary>
/// <param name="Id">Stable identifier, prefixed by the owning module ("users.total").</param>
/// <param name="Title">Short label shown above the value.</param>
/// <param name="Kind">How to render it.</param>
/// <param name="Value">Formatted primary value for a stat widget.</param>
/// <param name="Caption">Optional supporting line ("+3 this week").</param>
/// <param name="Items">Entries for a list widget.</param>
/// <param name="Link">Optional in-app route the tile navigates to.</param>
/// <param name="Order">Sort hint; lower comes first, ties broken by title.</param>
public sealed record DashboardWidget(
    string Id,
    string Title,
    DashboardWidgetKind Kind = DashboardWidgetKind.Stat,
    string? Value = null,
    string? Caption = null,
    IReadOnlyList<DashboardWidgetItem>? Items = null,
    string? Link = null,
    int Order = 100
);

/// <summary>
/// Lets a module contribute tiles to the dashboard.
///
/// The same seam pattern the framework uses everywhere else: a module
/// implements an interface declared in Application and is picked up
/// automatically, exactly as it contributes routes, events and token claims.
/// The Dashboard module therefore knows nothing about Users, Audit or
/// Notifications, and removing any of them removes its tiles with it — no
/// dangling references, no edits to a central list.
///
/// Providers must return only what the CALLER may see. The dashboard
/// aggregates them and cannot judge a tile's contents, so a provider that
/// leaks is a leak — see <c>modules/dashboard/README.md</c>.
/// </summary>
public interface IDashboardWidgetProvider
{
    /// <summary>
    /// Builds this module's tiles for the current caller.
    /// </summary>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    /// <returns>
    /// The tiles to show. Return an empty list when the caller may see none;
    /// never throw for "not permitted", or one module's restriction would
    /// take the whole dashboard down.
    /// </returns>
    Task<IReadOnlyList<DashboardWidget>> GetWidgetsAsync(
        CancellationToken cancellationToken = default
    );
}
