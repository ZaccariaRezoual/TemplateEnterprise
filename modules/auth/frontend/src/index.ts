// Ships this module's RouteMeta augmentation (`requiresAuth`) to consumers.
import "./types/router";

/**
 * Public surface of `@enterprise/module-auth`.
 *
 * The host app consumes exactly three things: `installAuthModule` at
 * bootstrap, `authRoutes` in its route registry and `useSessionStore` where
 * the UI reflects the session (e.g. the header). Everything else is internal.
 */
export { installAuthModule } from "./install";
export type { AuthModuleHost } from "./install";
export { authRoutes } from "./routes";
export { useSessionStore } from "./stores/session.store";
export type { AuthResponse, AuthUser } from "./api/auth.api";
