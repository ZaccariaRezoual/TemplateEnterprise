import type { RouteRecordRaw } from "vue-router";

/**
 * Routes owned by the Site module.
 *
 * None of them declares `requiresAuth`: they are the public face of the
 * application, so the host's area split leaves them at the root while
 * everything private is rebased under `/admin`.
 *
 * `publicSite: true` tells the application to wrap them in the public shell.
 *
 * `description` feeds the meta description and the link preview. Like the
 * title it is a fixed string here — a project that cares about its search
 * results rewrites both, and they are the two lines worth rewriting.
 *
 * `title` is a fixed string rather than a value read from `SiteContent`:
 * this array is built when the module is imported, which happens before the
 * host has provided its content, so reading it here would freeze the
 * placeholder text into every tab title. Per-page titles and descriptions are
 * the job of the SEO work in Fase 4, where they are set at navigation time.
 */
export const siteRoutes: RouteRecordRaw[] = [
  {
    path: "/",
    name: "site-home",
    component: () => import("./pages/HomePage.vue"),
    meta: {
      title: "Home",
      description:
        "Sostituisci questa descrizione in modules/site/frontend/src/routes.ts: è ciò che compare nei risultati di ricerca e nelle anteprime dei link.",
      publicSite: true,
    },
  },
  {
    path: "/about",
    name: "site-about",
    component: () => import("./pages/AboutPage.vue"),
    meta: { title: "Chi siamo", description: "Chi siamo e come lavoriamo.", publicSite: true },
  },
  {
    path: "/services",
    name: "site-services",
    component: () => import("./pages/ServicesPage.vue"),
    meta: { title: "Servizi", description: "I servizi che offriamo.", publicSite: true },
  },
  {
    path: "/contact",
    name: "site-contact",
    component: () => import("./pages/ContactPage.vue"),
    meta: {
      title: "Contatti",
      description: "Scrivici: rispondiamo il prima possibile.",
      publicSite: true,
    },
  },
  {
    path: "/privacy",
    name: "site-privacy",
    component: () => import("./pages/PrivacyPage.vue"),
    meta: { title: "Privacy", description: "Come trattiamo i dati personali.", publicSite: true },
  },
];
