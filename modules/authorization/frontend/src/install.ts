import type { ApiClient } from "@enterprise/sdk";
import type { App } from "vue";
import type { Router } from "vue-router";
import { installCanDirective } from "./directives/vCan";
import { installPermissionGuard } from "./guard";
import { provideAuthorizationApi, usePermissionsStore } from "./stores/permissions.store";

/** Integration seams the HOST exposes and this module plugs into. */
export interface AuthorizationModuleHost {
  /** The Vue application, which receives the `v-can` directive. */
  app: App;
  /** The application's router, which receives the permission guard. */
  router: Router;
  /** The application's configured SDK client. */
  api: ApiClient;
}

/**
 * Wires the Authorization module into the host application.
 *
 * Call once at bootstrap, after Pinia is installed and AFTER the auth module
 * (so authentication is resolved before permissions are checked). Loading the
 * permission set is left to the caller, so this function performs no I/O.
 *
 * @param host The host integration seams.
 */
export function installAuthorizationModule(host: AuthorizationModuleHost): void {
  provideAuthorizationApi(host.api);
  installCanDirective(host.app);
  installPermissionGuard(host.router);
}

/**
 * Loads or clears the permission set to match the session state.
 *
 * Called whenever authentication changes: after sign-in the permissions must
 * be fetched, and after sign-out dropped, so the next user never inherits the
 * previous one's actions.
 *
 * @param isAuthenticated Whether a session is active.
 */
export async function syncPermissions(isAuthenticated: boolean): Promise<void> {
  const permissions = usePermissionsStore();
  if (isAuthenticated) {
    await permissions.load();
  } else {
    permissions.clear();
  }
}
