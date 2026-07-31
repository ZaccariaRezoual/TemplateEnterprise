// Ships this module's RouteMeta augmentation (`permissions`) to every
// consumer: without it, a package importing this one would not see the field
// its own routes declare.
import "./types/router";

/**
 * Public surface of `@enterprise/module-authorization`.
 *
 * The host app calls `installAuthorizationModule` at bootstrap and
 * `syncPermissions` when the session changes; features use `usePermissions`
 * or the `v-can` directive.
 */
export { installAuthorizationModule, syncPermissions } from "./install";
export type { AuthorizationModuleHost } from "./install";
export { authorizationRoutes } from "./routes";
export { usePermissions } from "./composables/usePermissions";
export { provideAuthorizationApi, usePermissionsStore } from "./stores/permissions.store";
export type { UserAuthorization } from "./stores/permissions.store";
export { vCan } from "./directives/vCan";
