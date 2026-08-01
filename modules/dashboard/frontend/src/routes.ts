import type { RouteRecordRaw } from "vue-router";

/**
 * Routes owned by the Dashboard module.
 *
 * No `permissions` requirement: the dashboard itself is visible to any
 * authenticated user, and each widget provider decides server-side whether the
 * caller may see its tile. Gating the page would hide the whole overview from
 * a user who legitimately has one tile.
 */
export const dashboardRoutes: RouteRecordRaw[] = [
  {
    path: "/dashboard",
    name: "dashboard",
    component: () => import("./views/DashboardView.vue"),
    meta: { title: "Dashboard", requiresAuth: true },
  },
];
