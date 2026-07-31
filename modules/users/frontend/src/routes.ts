import type { RouteRecordRaw } from "vue-router";

/**
 * Routes owned by the Users module.
 *
 * `permissions` in the route meta is enforced by the Authorization module's
 * guard; declaring it here keeps the requirement next to the route it
 * protects instead of in a central table nobody updates.
 */
export const usersRoutes: RouteRecordRaw[] = [
  {
    path: "/users",
    name: "users",
    component: () => import("./pages/UsersPage.vue"),
    meta: { title: "Users", requiresAuth: true, permissions: ["users.read"] },
  },
];
