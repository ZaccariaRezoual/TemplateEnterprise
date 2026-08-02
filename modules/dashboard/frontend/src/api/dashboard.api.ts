import type { ApiClient, components } from "@enterprise/sdk";
import { executeSdkCall } from "@enterprise/shared";

/** A tile as returned by the API. */
export type DashboardWidget = components["schemas"]["DashboardWidget"];

let configuredApi: ApiClient | undefined;
let linkBase = "";

/**
 * Sets the prefix applied to a widget's `link`.
 *
 * A provider names a route it knows ("/users"); WHERE the application places
 * that route — at the root, or under a private area — is a decision of the
 * host, and the server has no business knowing it.
 *
 * @param base Prefix, e.g. "/admin". Empty string for none.
 */
export function provideDashboardLinkBase(base: string): void {
  linkBase = base;
}

/**
 * Resolves a widget link against the host's base.
 *
 * @param link Link as returned by the server, or null.
 * @returns The route to navigate to, or undefined when the widget has none.
 */
export function resolveWidgetLink(link: string | null | undefined): string | undefined {
  return link === null || link === undefined || link === "" ? undefined : `${linkBase}${link}`;
}

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
