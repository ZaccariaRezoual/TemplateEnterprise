import type { AuthGuardInstallation } from "@enterprise/module-auth";
import type { Router, RouteRecordRaw } from "vue-router";

/**
 * Base path of the private area. Everything that requires a session lives
 * under it, so a public page can never collide with an administrative one —
 * a project adding `/services` to its public site must not accidentally
 * shadow an admin screen.
 */
export const ADMIN_BASE = "/admin";

/**
 * Splits a set of module routes into the ones anyone may open and the ones
 * that require a session.
 *
 * The criterion is `meta.requiresAuth`, declared by each route next to the
 * code it protects — there is no central table of what is private, which is
 * exactly the table nobody would keep up to date.
 *
 * @param routes Routes contributed by features and modules.
 * @returns The public routes, and the private ones rebased under {@link ADMIN_BASE}.
 */
export function splitByArea(routes: readonly RouteRecordRaw[]): {
  publicRoutes: RouteRecordRaw[];
  adminRoutes: RouteRecordRaw[];
} {
  const publicRoutes: RouteRecordRaw[] = [];
  const adminRoutes: RouteRecordRaw[] = [];

  for (const route of routes) {
    if (route.meta?.requiresAuth === true) {
      adminRoutes.push({ ...route, path: `${ADMIN_BASE}${route.path}` });
    } else {
      publicRoutes.push(route);
    }
  }

  return { publicRoutes, adminRoutes };
}

/**
 * Registers the private area on the router.
 *
 * Requires proof that the Auth module installed its guard, and that
 * requirement is the whole point: `meta.requiresAuth` is enforced by that
 * guard and by nothing else. An application assembled without the Auth module
 * would otherwise carry an administrative shell that opens to anyone — the
 * API would still refuse the data, but the screens would be there, and no
 * test would fail. Demanding the token makes that assembly impossible to
 * compile rather than merely discouraged.
 *
 * @param router The application router.
 * @param routes Private routes, already rebased by {@link splitByArea}.
 * @param _installation Proof returned by `installAuthModule`; never read.
 */
export function registerAdminArea(
  router: Router,
  routes: readonly RouteRecordRaw[],
  // Underscored because it is never read: its VALUE is irrelevant, only the
  // fact that the caller had one to pass.
  _installation: AuthGuardInstallation,
): void {
  for (const route of routes) {
    router.addRoute(route);
  }

  // The root belongs to the public site when there is one. Only an
  // application assembled WITHOUT the Site module — an internal tool — sends
  // its root straight to the private area, and only then is there nothing
  // else it could show.
  if (!router.getRoutes().some((route) => route.path === "/")) {
    router.addRoute({ path: "/", redirect: ADMIN_BASE });
  }
}
