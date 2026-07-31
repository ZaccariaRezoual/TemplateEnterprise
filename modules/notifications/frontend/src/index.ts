import type { ApiClient } from "@enterprise/sdk";
import { provideNotificationsApi } from "./stores/notifications.store";

/**
 * Public surface of `@enterprise/module-notifications`.
 */
export { default as NotificationBell } from "./components/NotificationBell.vue";
export { provideNotificationsApi, useNotificationsStore } from "./stores/notifications.store";
export type { Notification } from "./stores/notifications.store";

/** Integration seams the HOST exposes and this module plugs into. */
export interface NotificationsModuleHost {
  /** The application's configured SDK client. */
  api: ApiClient;
}

/**
 * Wires the Notifications module into the host application. Call once at
 * bootstrap; loading is left to the bell component, which knows when it is
 * actually visible.
 *
 * @param host The host integration seams.
 */
export function installNotificationsModule(host: NotificationsModuleHost): void {
  provideNotificationsApi(host.api);
}
