# Dashboard Module

The application's landing page: a grid of tiles contributed by whichever
modules are installed.

It is the framework's composition made visible. The module knows nothing about
Users, Audit or Notifications — it asks the container for every
`IDashboardWidgetProvider` and renders what comes back. Disable a module and
its tiles disappear with it; add one and its tiles appear without a line
changing here or in the frontend.

## Contributing a tile

Implement `IDashboardWidgetProvider` (declared in `Application/Abstractions`)
inside your own module and register it there:

```csharp
internal sealed class OrdersWidgetProvider(IOrderRepository orders, ICurrentUser user)
    : IDashboardWidgetProvider
{
    public async Task<IReadOnlyList<DashboardWidget>> GetWidgetsAsync(CancellationToken ct)
    {
        // Not permitted is an EMPTY result, never an exception: the dashboard
        // shows what the caller may see and stays silent about the rest.
        if (!user.Permissions.Contains(Permissions.Orders.Read)) return [];

        return
        [
            new DashboardWidget(
                "orders.open",                       // id: prefix with your module
                "Open orders",
                Value: (await orders.CountOpenAsync(ct)).ToString(CultureInfo.InvariantCulture),
                Caption: "awaiting fulfilment",
                Link: "/orders",
                Order: 30),
        ];
    }
}

// in your module's ConfigureServices
services.AddScoped<IDashboardWidgetProvider, OrdersWidgetProvider>();
```

That is the entire integration. Your module references
`Application/Abstractions`, which it already does — **never** the Dashboard
project.

## Widget kinds

| Kind   | Renders as                        | Fields used                |
| ------ | --------------------------------- | -------------------------- |
| `Stat` | One large figure with a caption   | `Value`, `Caption`, `Link` |
| `List` | Up to a handful of recent entries | `Items`                    |

`Order` sorts the grid (lower first); ties break on title, so the layout is
stable between requests instead of shuffling. A `List` tile is given two grid
columns from `sm` up — a column of truncated labels at stat width is
unreadable.

## Design decisions

**Permissions are checked by the contributor, not by the endpoint.** Only the
owning module knows what its tile reveals. The endpoint requires nothing beyond
authentication, and the page itself declares no `permissions` in its route: a
user who legitimately has one tile should not be shown a forbidden page.

**A failing provider costs its tiles, not the dashboard.** Each provider is
called inside a `try`/`catch` in `DashboardWidgetAggregator`; a module whose
database is down is logged and skipped. A partial dashboard is far more useful
than an error page. Cancellation is deliberately re-thrown rather than
swallowed — a cancelled request must not look like a half-broken product.

**No aggregation logic in the endpoint.** `DashboardModule` maps the route;
`DashboardWidgetAggregator` does the work and is unit-tested directly.

**The client renders whatever arrives.** `DashboardGrid.vue` holds no list of
known widgets; it dispatches on `kind`. This is what makes the page a
composition point rather than a hard-coded overview that every new module has
to be added to.

**Realtime refresh invalidates, it does not patch.** A `notification.created`
event invalidates the widgets query and the server recomputes every tile
consistently — the client never has to know which tiles a given event affects.

## Endpoints

| Path                     | Purpose                                     |
| ------------------------ | ------------------------------------------- |
| `/api/dashboard/widgets` | Tiles visible to the caller (authenticated) |

## Frontend

`@enterprise/module-dashboard` exports `dashboardRoutes`,
`installDashboardModule({ api })`, `DashboardView` and `DashboardGrid`. The
host decides whether the dashboard is the landing page; `apps/web` redirects
`/` to it.
