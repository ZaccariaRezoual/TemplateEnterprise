import type { ApiClient } from "@enterprise/sdk";
import { provideUsersApi } from "./api/users.api";

/**
 * Public surface of `@enterprise/module-users`.
 */
export { usersRoutes } from "./routes";
export { useUsersList, usersKeys } from "./composables/useUsers";
export { provideUsersApi } from "./api/users.api";
export type { UserProfile, UserProfilePage } from "./api/users.api";

/** Integration seams the HOST exposes and this module plugs into. */
export interface UsersModuleHost {
  /** The application's configured SDK client. */
  api: ApiClient;
}

/**
 * Wires the Users module into the host application. Call once at bootstrap.
 *
 * @param host The host integration seams.
 */
export function installUsersModule(host: UsersModuleHost): void {
  provideUsersApi(host.api);
}
