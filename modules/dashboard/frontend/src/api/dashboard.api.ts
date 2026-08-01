import type { ApiClient, components } from "@enterprise/sdk";
import { executeSdkCall } from "@enterprise/shared";

/** A tile as returned by the API. */
export type DashboardWidget = components["schemas"]["DashboardWidget"];

let configuredApi: ApiClient | undefined;

/**
 * Injects the application's SDK client. Called once by `installDashboardModule`.
 *
 * @param api The configured SDK client.
 */
export function provideDashboardApi(api: ApiClient): void {
  configuredApi = api;
}

/**
 * Feature service of the Dashboard module.
 */
export const dashboardApi = {
  /**
   * Loads the tiles visible to the caller.
   *
   * The server decides which tiles exist and which the caller may see, so the
   * client renders whatever arrives — it holds no list of known widgets and
   * needs no change when a module starts or stops contributing one.
   *
   * @param signal Abort signal forwarded by TanStack Query on cancellation.
   * @returns The tiles, already ordered by the server.
   */
  widgets(signal?: AbortSignal): Promise<DashboardWidget[]> {
    if (configuredApi === undefined) {
      throw new Error(
        "Dashboard module is not installed. Call installDashboardModule() at bootstrap.",
      );
    }

    return executeSdkCall(() =>
      configuredApi!.GET("/api/dashboard/widgets", signal === undefined ? {} : { signal }),
    );
  },
};
