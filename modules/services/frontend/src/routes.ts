import type { RouteRecordRaw } from "vue-router";

/**
 * Administration routes owned by the Services module.
 *
 * `requiresAuth` is what the host's area split reads to rebase them under
 * `/admin`; `permissions` is enforced by the Authorization module's guard.
 * Both are declared next to the route they protect, rather than in a central
 * table nobody keeps up to date.
 *
 * Note the paths are written WITHOUT the `/admin` prefix: adding it here
 * would put it in twice, because rebasing is the host's job.
 */
export const servicesAdminRoutes: RouteRecordRaw[] = [
  {
    path: "/services",
    name: "services-admin",
    component: () => import("./pages/ServicesAdminPage.vue"),
    meta: { title: "Servizi", requiresAuth: true, permissions: ["services.read"] },
  },
  {
    path: "/services/new",
    name: "services-admin-new",
    component: () => import("./pages/ServiceEditPage.vue"),
    // Writing, not reading: the guard refuses someone who may only look.
    meta: { title: "Nuovo servizio", requiresAuth: true, permissions: ["services.write"] },
  },
  {
    path: "/services/:id",
    name: "services-admin-edit",
    component: () => import("./pages/ServiceEditPage.vue"),
    meta: { title: "Modifica servizio", requiresAuth: true, permissions: ["services.write"] },
  },
];

/**
 * Public routes owned by the Services module.
 *
 * They REPLACE the Site module's static "services" page: two routes cannot
 * share a path, and whoever owns the data owns its representation. The choice
 * is the composition root's — see `apps/web/src/router/index.ts` — so a
 * project that does not install this module keeps the static page, which is
 * exactly right for a site with nothing to book.
 *
 * `publicSite: true` puts them in the public shell. The titles here are fixed
 * strings; the detail page overwrites its own at runtime with the name of the
 * service, because the data is not known when this array is built.
 */
export const servicesPublicRoutes: RouteRecordRaw[] = [
  {
    path: "/services",
    name: "services-showcase",
    component: () => import("./pages/ServicesShowcasePage.vue"),
    meta: {
      title: "Servizi",
      description: "I servizi che offriamo, con durata e prezzo dove sono pubblicati.",
      publicSite: true,
    },
  },
  {
    path: "/services/:slug",
    name: "services-detail",
    component: () => import("./pages/ServiceDetailPage.vue"),
    meta: {
      title: "Servizio",
      description: "Dettaglio del servizio.",
      publicSite: true,
    },
  },
];
