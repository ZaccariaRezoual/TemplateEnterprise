import "vue-router";

declare module "vue-router" {
  /**
   * Route metadata OWNED by the Authorization module.
   *
   * Each module augments RouteMeta with the fields it owns, so a route's
   * requirements are declared by whoever enforces them. Removing the module
   * removes both the field and its guard.
   */
  interface RouteMeta {
    /**
     * Permissions required to enter the route; ALL of them must be held.
     * Enforced by the guard `installAuthorizationModule` installs.
     */
    permissions?: readonly string[];
  }
}

// A regular module (not a .d.ts) so consumers pick the augmentation up
// through a normal `import`, which travels with the package.
export {};
