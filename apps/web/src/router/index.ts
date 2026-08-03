import {
  appointmentsPrivateRoutes,
  appointmentsPublicRoutes,
} from "@enterprise/module-appointments";
import { authRoutes } from "@enterprise/module-auth";
import { authorizationRoutes } from "@enterprise/module-authorization";
import { dashboardRoutes } from "@enterprise/module-dashboard";
import { servicesAdminRoutes, servicesPublicRoutes } from "@enterprise/module-services";
import { siteRoutes } from "@enterprise/module-site";
import { usersRoutes } from "@enterprise/module-users";
import { createRouter, createWebHistory, type Router, type RouteRecordRaw } from "vue-router";
import { logger } from "@/core/logger/logger";
import { applySeo } from "@/core/seo/applySeo";
import { demoRoutes } from "@/features/demo/routes";
import { ADMIN_BASE, splitByArea } from "@/router/adminArea";

/**
 * Route registry.
 *
 * Features own their routes and export them from `features/<name>/routes.ts`;
 * module frontends export theirs from their package. This file only composes
 * them: adding a feature or module means adding one import here.
 *
 * The composition splits them into two AREAS by `meta.requiresAuth`:
 * everything private is rebased under `/admin` and registered separately, by
 * `registerAdminArea`, which the bootstrap can only call after the Auth
 * module has installed its guard (see `adminArea.ts`).
 */
/**
 * Name of the Site module's static "services" page.
 *
 * Two routes cannot share a path, and both modules want `/services`: the Site
 * module has a page written in `site.config.ts`, the Services module serves
 * the catalogue from data. Whoever owns the DATA owns its representation, so
 * when the Services module is installed its pages win — and a project that
 * does not install it keeps the static page, which is exactly right for a
 * site with nothing to book.
 *
 * The choice belongs here, in the composition root: neither module may decide
 * it, because neither knows whether the other exists.
 */
const STATIC_SERVICES_ROUTE = "site-services";

const contributedRoutes: RouteRecordRaw[] = [
  ...siteRoutes.filter((route) => route.name !== STATIC_SERVICES_ROUTE),
  ...servicesPublicRoutes,
  ...dashboardRoutes,
  ...demoRoutes,
  ...authRoutes,
  ...authorizationRoutes,
  ...usersRoutes,
  ...servicesAdminRoutes,
  ...appointmentsPublicRoutes,
  ...appointmentsPrivateRoutes,
];

const { publicRoutes, adminRoutes } = splitByArea(contributedRoutes);

/**
 * Private routes, rebased under `/admin`, plus the landing redirect of the
 * area. Which screen greets a signed-in user is the application's decision,
 * not the Dashboard module's — so it lives here rather than in the module.
 *
 * Registered by `registerAdminArea`.
 */
export const privateRoutes: readonly RouteRecordRaw[] = [
  ...adminRoutes,
  { path: ADMIN_BASE, redirect: `${ADMIN_BASE}/dashboard` },
];

const routes: RouteRecordRaw[] = [
  ...publicRoutes,
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
 * It registers only the routes anyone may open. The private area is added by
 * `registerAdminArea`, so an application assembled without the Auth module
 * simply has no administrative screens — rather than having them unguarded.
 *
 * Guards implemented here:
 * - Document metadata (title, description, canonical, Open Graph) from
 *   `route.meta`.
 *
 * The authentication and permission guards are installed by the Auth and
 * Authorization modules; they hook into `meta.requiresAuth` /
 * `meta.permissions`, typed by those modules.
 *
 * @returns The configured router instance.
 */
export function createAppRouter(): Router {
  const router = createRouter({
    history: createWebHistory(),
    routes,
    scrollBehavior: (_to, _from, savedPosition) => savedPosition ?? { top: 0 },
  });

  // Title, description, canonical and Open Graph tags, on every navigation:
  // a page nobody remembered to think about still gets them.
  router.afterEach((to) => {
    applySeo(to);
  });

  router.onError((error) => {
    logger.error("Navigation failed", { error });
  });

  return router;
}
