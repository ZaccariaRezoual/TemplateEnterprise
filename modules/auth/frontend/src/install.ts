import type { ApiClient } from "@enterprise/sdk";
import type { Router } from "vue-router";
import { AuthApi } from "./api/auth.api";
import { installAuthGuard } from "./guard";
import { provideAuthApi, useSessionStore } from "./stores/session.store";

/** Integration seams the HOST exposes and this module plugs into. */
export interface AuthModuleHost {
  /** The application's configured SDK client. */
  api: ApiClient;
  /** The application's router (receives the auth guard). */
  router: Router;
  /** Registers the source of the Bearer token on the app's HTTP layer. */
  setAuthTokenProvider: (provider: () => string | undefined) => void;
  /** Registers the 401-recovery handler on the app's HTTP layer. */
  setUnauthorizedHandler: (handler: () => Promise<boolean>) => void;
}

/**
 * Wires the Auth module into the host application.
 *
 * Call once at bootstrap, AFTER Pinia is installed. The direction matters:
 * the module plugs into seams that core defines, so removing the module from
 * the app means deleting this call and the route import — core never knows
 * the module existed.
 *
 * @param host The host integration seams.
 */
export function installAuthModule(host: AuthModuleHost): void {
  provideAuthApi(new AuthApi(host.api));

  const session = useSessionStore();
  host.setAuthTokenProvider(() => session.accessToken);
  host.setUnauthorizedHandler(() => session.tryRefresh());
  installAuthGuard(host.router);
}
