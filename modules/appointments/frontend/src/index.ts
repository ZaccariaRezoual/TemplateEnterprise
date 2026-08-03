import type { ApiClient } from "@enterprise/sdk";
import { provideAppointmentsApi } from "./api/appointments.api";
import { provideBookingSession, type BookingSession } from "./session";

/**
 * Public surface of `@enterprise/module-appointments`.
 *
 * The host consumes three things: `installAppointmentsModule` at bootstrap,
 * and the two route arrays in its registry.
 */
export { appointmentsPublicRoutes, appointmentsPrivateRoutes } from "./routes";
export {
  appointmentsApi,
  appointmentsApiOrigin,
  provideAppointmentsApi,
  type AdminAppointment,
  type Availability,
  type AvailabilitySettings,
  type AppointmentStatus,
  type MyAppointment,
  type Slot,
} from "./api/appointments.api";
export {
  appointmentsKeys,
  useAppointmentActions,
  useAvailability,
  useAvailabilitySettings,
  useCalendar,
  useCancelMyAppointment,
  useMyAppointments,
  useRequestAppointment,
} from "./composables/useAppointments";
export { default as AppointmentCalendar } from "./components/AppointmentCalendar.vue";
export { default as AddToCalendar } from "./components/AddToCalendar.vue";

export { useBookingSession } from "./session";
export type { BookingSession } from "./session";

/** Integration seams the HOST exposes and this module plugs into. */
export interface AppointmentsModuleHost {
  /** The application's configured SDK client. */
  api: ApiClient;
  /**
   * Origin of the API, used for the calendar-file link the browser follows
   * directly. Omit it for a same-origin deployment.
   */
  apiOrigin?: string | undefined;
  /**
   * How the booking page finds out whether somebody is signed in, and where
   * to send them if not.
   *
   * Asked of the HOST rather than read from the Auth module, for the same
   * reason the public site asks: this module must keep working in an
   * application assembled without authentication, and a module that imports
   * another module is a module you cannot remove.
   *
   * Omit it and the booking flow assumes nobody is signed in and points at
   * `/login`, which is right for the default assembly.
   */
  session?: BookingSession | undefined;
}

/**
 * Wires the Appointments module into the host application. Call once at
 * bootstrap.
 *
 * @param host The host integration seams.
 */
export function installAppointmentsModule(host: AppointmentsModuleHost): void {
  provideAppointmentsApi(host.api, host.apiOrigin ?? "");
  provideBookingSession(host.session);
}
