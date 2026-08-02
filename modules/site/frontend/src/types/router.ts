import "vue-router";

declare module "vue-router" {
  /**
   * Route metadata owned by the SITE module.
   *
   * Declared here, next to the code that reads it, exactly as Auth declares
   * `requiresAuth` and Authorization declares `permissions`: a route's
   * requirements always live with the module that enforces them.
   */
  interface RouteMeta {
    /**
     * Marks a route as part of the public site, so the application wraps it
     * in the public shell instead of the administrative one.
     *
     * It is a MARKER, not a permission: what makes a route private is
     * `requiresAuth`, enforced by the Auth module's guard.
     */
    publicSite?: boolean;
  }
}
