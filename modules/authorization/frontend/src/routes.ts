import type { RouteRecordRaw } from "vue-router";

/**
 * Routes owned by the Authorization module.
 *
 * The forbidden screen lives here because this module decides what
 * "forbidden" means; the guard redirects to it by name, so the host never
 * hardcodes the path.
 */
export const authorizationRoutes: RouteRecordRaw[] = [
  {
    path: "/forbidden",
    name: "forbidden",
    component: () => import("./pages/ForbiddenPage.vue"),
    meta: { title: "Not allowed" },
  },
];
