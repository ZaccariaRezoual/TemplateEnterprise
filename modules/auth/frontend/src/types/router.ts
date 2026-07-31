import "vue-router";

declare module "vue-router" {
  /**
   * Route metadata OWNED by the Auth module.
   *
   * Each module augments RouteMeta with the fields it owns, so a route's
   * requirements are declared by whoever enforces them.
   */
  interface RouteMeta {
    /**
     * Whether an authenticated session is required.
     * Enforced by the guard `installAuthModule` installs.
     */
    requiresAuth?: boolean;
  }
}

// A regular module (not a .d.ts) so consumers pick the augmentation up
// through a normal `import`, which travels with the package.
export {};
