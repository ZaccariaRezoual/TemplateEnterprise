import type { ApiClient } from "@enterprise/sdk";
import type { Router } from "vue-router";
import { AuthApi } from "./api/auth.api";
import { installAuthGuard } from "./guard";
import { setHomePath } from "./home";
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
  /**
   * Where a signed-in user belongs: used after sign-in and when an
   * authenticated user opens /login. Defaults to "/".
   *
   * The module does not assume it: an application with a public site sends
   * people to its private area, one without sends them home, and only the
   * host knows which it is.
   */
  homePath?: string | undefined;
}

/**
 * Proof that the authentication guard is installed on the router.
 *
 * It exists to be *required* by whatever registers routes that must never be
 * reachable without authentication. `meta.requiresAuth` is enforced by this
 * module's guard and by nothing else: without the module the flag is inert
 * and a private area would open to anyone. Handing back a token the caller
 * must present turns that from a convention into a compile error.
 */
export interface AuthGuardInstallation {
  /** Marker; carries no data. */
  readonly guardInstalled: true;
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
 * @returns Proof that the guard is installed, to be presented when
 * registering routes that require authentication.
 */
export function installAuthModule(host: AuthModuleHost): AuthGuardInstallation {
  provideAuthApi(new AuthApi(host.api));

  const session = useSessionStore();
  host.setAuthTokenProvider(() => session.accessToken);
  host.setUnauthorizedHandler(() => session.tryRefresh());

  if (host.homePath !== undefined) {
    setHomePath(host.homePath);
  }

  installAuthGuard(host.router);

  return { guardInstalled: true };
}
