import type { RouteRecordRaw } from "vue-router";

/**
 * Routes owned by the Auth module. The host app composes them exactly like a
 * feature's routes; pages are lazy-loaded into their own chunk.
 *
 * `/login` and `/register` use the blank layout (no app chrome before a
 * session exists); `/account` demonstrates a protected route.
 */
export const authRoutes: RouteRecordRaw[] = [
  {
    path: "/login",
    name: "login",
    component: () => import("./pages/LoginPage.vue"),
    meta: { title: "Sign in", layout: "blank" },
  },
  {
    path: "/register",
    name: "register",
    component: () => import("./pages/RegisterPage.vue"),
    meta: { title: "Create account", layout: "blank" },
  },
  {
    path: "/account",
    name: "account",
    component: () => import("./pages/AccountPage.vue"),
    meta: { title: "Account", requiresAuth: true },
  },
];
