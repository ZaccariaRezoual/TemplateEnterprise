import "vue-router";

declare module "vue-router" {
  /**
   * Typed route metadata shared by every feature.
   *
   * `requiresAuth` and `permissions` are declared now and enforced by the
   * guards the Auth module installs in Fase 4, so features can already
   * annotate their routes correctly.
   */
  interface RouteMeta {
    /** Document title shown for this route. */
    title?: string;
    /** Layout wrapping the page; defaults to "default". Use "blank" for login/error screens. */
    layout?: "default" | "blank";
    /** Whether an authenticated session is required. */
    requiresAuth?: boolean;
    /** Permissions the user must hold to enter the route (PBAC). */
    permissions?: readonly string[];
  }
}
