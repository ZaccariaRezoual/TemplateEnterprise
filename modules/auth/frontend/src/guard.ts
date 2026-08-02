import type { Router } from "vue-router";
import { getHomePath } from "./home";
import { useSessionStore } from "./stores/session.store";

/**
 * Installs the authentication guard on the host router.
 *
 * Behavior:
 * - Waits for the startup session restore, so a user with a valid refresh
 *   cookie is never bounced to /login by a race.
 * - Routes with `meta.requiresAuth` redirect anonymous users to /login,
 *   remembering the destination in the `redirect` query for post-login return.
 * - Signed-in users visiting /login or /register are sent to the host's home
 *   path: those pages have no meaning inside a session.
 *
 * The permission guard (meta.permissions, PBAC) is added by the Permissions
 * module in Fase 5.
 *
 * @param router The host application's router.
 */
export function installAuthGuard(router: Router): void {
  router.beforeEach(async (to) => {
    const session = useSessionStore();

    if (!session.isRestored) {
      await session.restore();
    }

    if (to.meta.requiresAuth === true && !session.isAuthenticated) {
      return { name: "login", query: { redirect: to.fullPath } };
    }

    if ((to.name === "login" || to.name === "register") && session.isAuthenticated) {
      return { path: getHomePath() };
    }

    return true;
  });
}
