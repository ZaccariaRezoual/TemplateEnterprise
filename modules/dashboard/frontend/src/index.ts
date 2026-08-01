import type { ApiClient } from "@enterprise/sdk";
import { provideDashboardApi } from "./api/dashboard.api";

/**
 * Public surface of `@enterprise/module-dashboard`.
 */
export { default as DashboardGrid } from "./components/DashboardGrid.vue";
export { default as DashboardView } from "./views/DashboardView.vue";
export { dashboardKeys, useDashboardWidgets } from "./composables/useDashboard";
export { provideDashboardApi } from "./api/dashboard.api";
export type { DashboardWidget } from "./api/dashboard.api";
export { dashboardRoutes } from "./routes";

/** Integration seams the HOST exposes and this module plugs into. */
export interface DashboardModuleHost {
  /** The application's configured SDK client. */
  api: ApiClient;
}

/**
 * Wires the Dashboard module into the host application. Call once at bootstrap.
 *
 * The module exports `dashboardRoutes`, but whether the application makes the
 * dashboard its landing page is the host's decision, not the module's.
 *
 * @param host The host integration seams.
 */
export function installDashboardModule(host: DashboardModuleHost): void {
  provideDashboardApi(host.api);
}
