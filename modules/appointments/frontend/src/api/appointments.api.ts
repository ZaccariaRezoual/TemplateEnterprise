import type { ApiClient, components } from "@enterprise/sdk";
import { executeSdkCall } from "@enterprise/shared";

/** The bookable slots of a service, day by day. */
export type Availability = components["schemas"]["AvailabilityDto"];

/** One bookable slot. */
export type Slot = components["schemas"]["SlotDto"];

/** An appointment as its owner sees it. */
export type MyAppointment = components["schemas"]["MyAppointmentDto"];

/** An appointment as the administration sees it. */
export type AdminAppointment = components["schemas"]["AdminAppointmentDto"];

/** Where an appointment is in its life. */
export type AppointmentStatus = components["schemas"]["AppointmentStatus"];

/** The weekly opening hours and their exceptions. */
export type AvailabilitySettings = components["schemas"]["AvailabilitySettingsDto"];

let configuredApi: ApiClient | undefined;

/**
 * Origin of the API, used to build the `.ics` download link.
 *
 * Empty for a same-origin deployment, which is the normal case: the SDK's
 * paths already carry the full route.
 */
let apiOrigin = "";

/**
 * Injects the application's SDK client and API origin. Called once by
 * `installAppointmentsModule`.
 *
 * @param api The configured SDK client.
 * @param origin Origin of the API, or an empty string when same-origin.
 */
export function provideAppointmentsApi(api: ApiClient, origin = ""): void {
  configuredApi = api;
  apiOrigin = origin;
}

/**
 * Returns the API origin, for the links this module hands to a browser rather
 * than fetching itself — the calendar file is downloaded by the person, not
 * by the SDK.
 *
 * @returns The origin, or an empty string when same-origin.
 */
export function appointmentsApiOrigin(): string {
  return apiOrigin;
}

function requireApi(): ApiClient {
  if (configuredApi === undefined) {
    throw new Error(
      "Appointments module is not installed. Call installAppointmentsModule() at bootstrap.",
    );
  }
  return configuredApi;
}

/**
 * Feature service of the Appointments module: the only place that knows which
 * API operations it uses. Components go through composables, never here.
 */
export const appointmentsApi = {
  /**
   * Asks when a service can be booked.
   *
   * Anonymous: the visitor picks a time before being asked to sign in. The
   * answer carries instants and nothing else — it can say an hour is taken,
   * never by whom.
   *
   * @param serviceId Service to check.
   * @param from First local date, "yyyy-MM-dd".
   * @param to Last local date, "yyyy-MM-dd".
   * @param signal Abort signal forwarded by TanStack Query on cancellation.
   * @returns One entry per day of the range, closed days included.
   */
  availability(
    serviceId: string,
    from: string,
    to: string,
    signal?: AbortSignal,
  ): Promise<Availability> {
    return executeSdkCall(() =>
      requireApi().GET("/api/appointments/availability", {
        params: { query: { serviceId, from, to } },
        ...(signal === undefined ? {} : { signal }),
      }),
    );
  },

  /**
   * Asks for an appointment.
   *
   * It is a REQUEST: nothing is reserved until an administrator confirms it,
   * and every message the page shows has to say so.
   *
   * @param input Service, chosen instant, phone number and optional note.
   * @returns The appointment, in `Requested` state.
   * @throws {import("@enterprise/shared").BusinessError} When the slot is no longer offered.
   * @throws {import("@enterprise/shared").UnauthorizedError} When nobody is signed in.
   */
  request(input: {
    serviceId: string;
    startUtc: string;
    contactPhone: string;
    customerNote?: string | undefined;
  }): Promise<MyAppointment> {
    return executeSdkCall(() =>
      requireApi().POST("/api/appointments", {
        body: {
          serviceId: input.serviceId,
          startUtc: input.startUtc,
          contactPhone: input.contactPhone,
          customerNote: input.customerNote ?? null,
        },
      }),
    );
  },

  /**
   * Lists the caller's own appointments.
   *
   * It takes no identifier: the caller comes from the token, so there is
   * nothing for anyone to change into somebody else's.
   *
   * @param includePast Whether to include appointments already over.
   * @param signal Abort signal forwarded by TanStack Query on cancellation.
   * @returns The appointments, soonest first.
   */
  mine(includePast: boolean, signal?: AbortSignal): Promise<MyAppointment[]> {
    return executeSdkCall(() =>
      requireApi().GET("/api/appointments/mine", {
        params: { query: { includePast } },
        ...(signal === undefined ? {} : { signal }),
      }),
    );
  },

  /**
   * Cancels one of the caller's own appointments.
   *
   * @param id Appointment to call off.
   * @param reason Optional explanation.
   * @throws {import("@enterprise/shared").BusinessError} Past the cutoff, or in a state that forbids it.
   */
  cancelMine(id: string, reason?: string): Promise<void> {
    return executeSdkCall(() =>
      requireApi().POST("/api/appointments/{id}/cancel", {
        params: { path: { id } },
        body: { reason: reason ?? null },
      }),
    );
  },

  /**
   * Lists the appointments overlapping a window, for the calendar.
   *
   * @param fromUtc Start of the window, ISO 8601 UTC.
   * @param toUtc End of the window, ISO 8601 UTC.
   * @param signal Abort signal forwarded by TanStack Query on cancellation.
   * @returns The appointments, soonest first.
   * @throws {import("@enterprise/shared").ForbiddenError} Without appointments.read.
   */
  calendar(fromUtc: string, toUtc: string, signal?: AbortSignal): Promise<AdminAppointment[]> {
    return executeSdkCall(() =>
      requireApi().GET("/api/admin/appointments", {
        params: { query: { from: fromUtc, to: toUtc } },
        ...(signal === undefined ? {} : { signal }),
      }),
    );
  },

  /**
   * Confirms a request, cancelling the ones it displaces.
   *
   * @param id Request to accept.
   * @returns The confirmed appointment.
   * @throws {import("@enterprise/shared").BusinessError} When the slot was taken in the meantime.
   */
  confirm(id: string): Promise<AdminAppointment> {
    return executeSdkCall(() =>
      requireApi().POST("/api/admin/appointments/{id}/confirm", {
        params: { path: { id } },
      }),
    );
  },

  /**
   * Moves an appointment to another time.
   *
   * The server revalidates: a drag in the calendar is an intention, not an
   * authorization, so the caller must be ready for a refusal.
   *
   * @param id Appointment to move.
   * @param startUtc New start instant, ISO 8601 UTC.
   * @returns The moved appointment.
   * @throws {import("@enterprise/shared").BusinessError} When the target time is occupied.
   */
  reschedule(id: string, startUtc: string): Promise<AdminAppointment> {
    return executeSdkCall(() =>
      requireApi().POST("/api/admin/appointments/{id}/reschedule", {
        params: { path: { id } },
        body: { startUtc },
      }),
    );
  },

  /**
   * Cancels an appointment on behalf of the business.
   *
   * @param id Appointment to call off.
   * @param reason Why. Required: the customer reads it.
   */
  cancel(id: string, reason: string): Promise<void> {
    return executeSdkCall(() =>
      requireApi().POST("/api/admin/appointments/{id}/cancel", {
        params: { path: { id } },
        body: { reason },
      }),
    );
  },

  /**
   * Reads the weekly opening hours and their exceptions.
   *
   * @param signal Abort signal forwarded by TanStack Query on cancellation.
   * @returns The configuration.
   */
  settings(signal?: AbortSignal): Promise<AvailabilitySettings> {
    return executeSdkCall(() =>
      requireApi().GET("/api/admin/appointments/availability", {
        ...(signal === undefined ? {} : { signal }),
      }),
    );
  },

  /**
   * Replaces the weekly opening hours and their exceptions.
   *
   * A wholesale replace: whatever is absent from the document was removed on
   * purpose.
   *
   * @param settings The complete configuration.
   * @returns The saved configuration, read back.
   */
  saveSettings(settings: AvailabilitySettings): Promise<AvailabilitySettings> {
    return executeSdkCall(() =>
      requireApi().PUT("/api/admin/appointments/availability", {
        body: { rules: settings.rules, exceptions: settings.exceptions },
      }),
    );
  },
};
