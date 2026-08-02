import type { RouteRecordRaw } from "vue-router";

/**
 * Routes owned by the Demo feature.
 *
 * Every feature exports its own routes and the central registry
 * (`router/index.ts`) only composes them — that is what keeps features
 * independently removable. Pages are lazy-loaded so each feature ships as its
 * own chunk.
 */
export const demoRoutes: RouteRecordRaw[] = [
  {
    path: "/demo",
    name: "demo",
    component: () => import("@/features/demo/pages/DemoPage.vue"),
    // Private: it is the framework's playground, and one of its actions
    // notifies the CALLER, so there has to be one. Without this flag the page
    // would stay outside /admin and a project's visitors could land on it.
    meta: { title: "Demo", requiresAuth: true },
  },
];
