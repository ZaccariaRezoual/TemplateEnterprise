import type { RouteRecordRaw } from "vue-router";

/**
 * The public booking flow.
 *
 * `publicSite: true` puts it in the public shell, and there is deliberately
 * no `requiresAuth`: the visitor picks a service, a day and an hour BEFORE
 * being asked for an account. Guarding the route would put the sign-in screen
 * first, which is the single most effective way to lose a booking.
 *
 * The account is asked for on the last step, and the chosen slot survives it
 * because it lives in the page's own state.
 *
 * It is NOT prerendered: the page is a form over live data, so a static
 * capture would be a snapshot of yesterday's availability. Only the service
 * pages that link INTO it are worth prerendering.
 */
export const appointmentsPublicRoutes: RouteRecordRaw[] = [
  {
    path: "/book/:slug",
    name: "appointments-book",
    component: () => import("./pages/BookingPage.vue"),
    meta: {
      title: "Prenota",
      description: "Scegli il giorno e l'ora del tuo appuntamento.",
      publicSite: true,
    },
  },
];

/**
 * Routes that need a session.
 *
 * They split into two kinds, and the difference is the whole reason they are
 * declared separately:
 *
 * - `/my-appointments` requires a session and **no permission**. These are
 *   the person's own data, and a permission would be the wrong tool for "is
 *   this mine?" — the endpoint reads the caller from the token.
 * - the calendar and the opening hours require the appointments permissions,
 *   because they are about everybody's bookings.
 *
 * The paths carry no `/admin` prefix: rebasing is the host's job, and writing
 * it here would put it in twice.
 */
export const appointmentsPrivateRoutes: RouteRecordRaw[] = [
  {
    path: "/my-appointments",
    name: "appointments-mine",
    component: () => import("./pages/MyAppointmentsPage.vue"),
    meta: { title: "I miei appuntamenti", requiresAuth: true },
  },
  {
    path: "/appointments",
    name: "appointments-admin",
    component: () => import("./pages/AppointmentsAdminPage.vue"),
    meta: { title: "Appuntamenti", requiresAuth: true, permissions: ["appointments.read"] },
  },
  {
    path: "/appointments/availability",
    name: "appointments-availability",
    component: () => import("./pages/AvailabilitySettingsPage.vue"),
    meta: {
      title: "Orari di apertura",
      requiresAuth: true,
      permissions: ["appointments.write"],
    },
  },
];
