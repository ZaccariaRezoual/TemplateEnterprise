import "vue-router";

declare module "vue-router" {
  /**
   * Route metadata owned by the APPLICATION.
   *
   * Module-specific fields are declared by the modules themselves
   * (`requiresAuth` by Auth, `permissions` by Authorization), so a route's
   * requirements always live with the code that enforces them.
   */
  interface RouteMeta {
    /** Document title shown for this route. */
    title?: string;
    /** Layout wrapping the page; defaults to "default". Use "blank" for login/error screens. */
    layout?: "default" | "blank";
    /**
     * Meta description of the page, used for search results and link
     * previews. Worth writing for public routes; pointless for private ones,
     * which no crawler will ever reach.
     */
    description?: string;
  }
}
