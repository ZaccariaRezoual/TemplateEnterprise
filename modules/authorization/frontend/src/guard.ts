import type { Router } from "vue-router";
import { usePermissionsStore } from "./stores/permissions.store";

/**
 * Installs the permission guard on the host router.
 *
 * A route declaring `meta.permissions` requires ALL of them. When the caller
 * is short, navigation is refused and the user is sent to the forbidden
 * screen rather than to a page that would render empty and read as a bug.
 *
 * Install it AFTER the auth guard, so authentication is resolved first: an
 * anonymous user must be asked to sign in, not told they lack permissions.
 *
 * Like every client-side check this only shapes navigation; the API enforces
 * the same permissions on each request.
 *
 * @param router The host application's router.
 */
export function installPermissionGuard(router: Router): void {
  router.beforeEach(async (to) => {
    const required = to.meta.permissions;
    if (required === undefined || required.length === 0) {
      return true;
    }

    const permissions = usePermissionsStore();
    if (!permissions.isLoaded) {
      await permissions.load();
    }

    return required.every((permission) => permissions.can(permission))
      ? true
      : { name: "forbidden", query: { from: to.fullPath } };
  });
}
