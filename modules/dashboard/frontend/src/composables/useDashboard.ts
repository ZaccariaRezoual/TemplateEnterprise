import { useQuery } from "@tanstack/vue-query";
import { useRealtimeInvalidation } from "@enterprise/module-realtime";
import { dashboardApi, type DashboardWidget } from "../api/dashboard.api";

/** Query keys of the Dashboard module. */
export const dashboardKeys = {
  all: ["dashboard"] as const,
  widgets: () => [...dashboardKeys.all, "widgets"] as const,
};

/**
 * Reads the dashboard tiles.
 *
 * Server state, so TanStack Query owns it. A new notification invalidates the
 * query rather than patching a counter: the server recomputes every tile
 * consistently, and the client never has to know which tiles a given event
 * affects — that coupling is exactly what a widget system exists to avoid.
 *
 * @returns The query result: `data`, `isPending`, `isError`, `error`, `refetch`.
 */
export function useDashboardWidgets() {
  const query = useQuery<DashboardWidget[]>({
    queryKey: dashboardKeys.widgets(),
    queryFn: ({ signal }) => dashboardApi.widgets(signal),
  });

  useRealtimeInvalidation("notification.created", dashboardKeys.widgets());

  return query;
}
