import { authRoutes } from "@enterprise/module-auth";
import { authorizationRoutes } from "@enterprise/module-authorization";
import { dashboardRoutes } from "@enterprise/module-dashboard";
import { usersRoutes } from "@enterprise/module-users";
import { createRouter, createWebHistory, type Router, type RouteRecordRaw } from "vue-router";
import { logger } from "@/core/logger/logger";
import { demoRoutes } from "@/features/demo/routes";

/**
 * Route registry.
 *
 * Features own their routes and export them from `features/<name>/routes.ts`;
 * module frontends export theirs from their package. This file only composes
 * them: adding a feature or module means adding one import here.
 */
const routes: RouteRecordRaw[] = [
  { path: "/", redirect: "/dashboard" },
  ...dashboardRoutes,
  ...demoRoutes,
  ...authRoutes,
  ...authorizationRoutes,
  ...usersRoutes,
  {
    path: "/:pathMatch(.*)*",
    name: "not-found",
    component: () => import("@/shared/components/NotFoundPage.vue"),
    meta: { title: "Page not found" },
  },
];

/**
 * Creates the application router with the framework navigation guards.
 *
 * Guards implemented here:
 * - Document title from `route.meta.title`.
 *
 * The authentication and permission guards are added by the Auth module in
 * Fase 4; they hook into `meta.requiresAuth` / `meta.permissions`, already
 * typed in `types/router.d.ts`.
 *
 * @returns The configured router instance.
 */
export function createAppRouter(): Router {
  const router = createRouter({
    history: createWebHistory(),
    routes,
    scrollBehavior: (_to, _from, savedPosition) => savedPosition ?? { top: 0 },
  });

  router.beforeEach((to) => {
    document.title = to.meta.title === undefined ? "Enterprise Framework" : `${to.meta.title}`;
  });

  router.onError((error) => {
    logger.error("Navigation failed", { error });
  });

  return router;
}
