import { useMutation, useQuery, useQueryClient } from "@tanstack/vue-query";
import { computed, type Ref } from "vue";
import {
  appointmentsApi,
  type AdminAppointment,
  type Availability,
  type AvailabilitySettings,
  type MyAppointment,
} from "../api/appointments.api";

/**
 * Query keys of the Appointments module.
 *
 * Centralized so cache invalidation never relies on a hand-typed string, and
 * so the split between what a customer may read and what the calendar reads
 * is visible: they are different data with different visibility rules, and
 * one must never satisfy a request for the other.
 */
export const appointmentsKeys = {
  all: ["appointments"] as const,
  availability: (serviceId: string, from: string, to: string) =>
    [...appointmentsKeys.all, "availability", serviceId, from, to] as const,
  mine: (includePast: boolean) => [...appointmentsKeys.all, "mine", includePast] as const,
  calendar: (fromUtc: string, toUtc: string) =>
    [...appointmentsKeys.all, "calendar", fromUtc, toUtc] as const,
  settings: () => [...appointmentsKeys.all, "settings"] as const,
};

/**
 * Reads the bookable slots of a service over a range of local dates.
 *
 * @param serviceId Reactive service identifier.
 * @param from Reactive first local date, "yyyy-MM-dd".
 * @param to Reactive last local date, "yyyy-MM-dd".
 * @returns The query result.
 */
export function useAvailability(serviceId: Ref<string>, from: Ref<string>, to: Ref<string>) {
  return useQuery<Availability>({
    queryKey: computed(() => appointmentsKeys.availability(serviceId.value, from.value, to.value)),
    queryFn: ({ signal }) =>
      appointmentsApi.availability(serviceId.value, from.value, to.value, signal),
    enabled: computed(() => serviceId.value !== ""),
    // Availability goes stale the moment somebody else books: a cached answer
    // that says "free" is exactly the one that produces a refused booking.
    staleTime: 0,
    retry: false,
  });
}

/**
 * Asks for an appointment.
 *
 * On success it invalidates everything: the customer's own list gained an
 * entry, and the availability they were looking at is now a slot poorer for
 * whoever else is looking.
 *
 * @returns The mutation: `mutateAsync`, `isPending`, `error`.
 */
export function useRequestAppointment() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (input: {
      serviceId: string;
      startUtc: string;
      contactPhone: string;
      customerNote?: string | undefined;
    }) => appointmentsApi.request(input),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: appointmentsKeys.all }),
  });
}

/**
 * Reads the caller's own appointments.
 *
 * @param includePast Reactive flag; the default view is about the future.
 * @returns The query result.
 */
export function useMyAppointments(includePast: Ref<boolean>) {
  return useQuery<MyAppointment[]>({
    queryKey: computed(() => appointmentsKeys.mine(includePast.value)),
    queryFn: ({ signal }) => appointmentsApi.mine(includePast.value, signal),
  });
}

/**
 * Cancels one of the caller's own appointments.
 *
 * @returns The mutation.
 */
export function useCancelMyAppointment() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (input: { id: string; reason?: string | undefined }) =>
      appointmentsApi.cancelMine(input.id, input.reason),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: appointmentsKeys.all }),
  });
}

/**
 * Reads the appointments of a window, for the calendar.
 *
 * @param fromUtc Reactive start of the window, ISO 8601 UTC.
 * @param toUtc Reactive end of the window, ISO 8601 UTC.
 * @returns The query result.
 */
export function useCalendar(fromUtc: Ref<string>, toUtc: Ref<string>) {
  return useQuery<AdminAppointment[]>({
    queryKey: computed(() => appointmentsKeys.calendar(fromUtc.value, toUtc.value)),
    queryFn: ({ signal }) => appointmentsApi.calendar(fromUtc.value, toUtc.value, signal),
    enabled: computed(() => fromUtc.value !== "" && toUtc.value !== ""),
  });
}

/**
 * The three things an administrator does to an appointment.
 *
 * Bundled because they share one invalidation rule: any of them changes what
 * the calendar shows AND what the public may still book.
 *
 * @returns `confirm`, `reschedule` and `cancel` mutations.
 */
export function useAppointmentActions() {
  const queryClient = useQueryClient();
  const refresh = () => {
    void queryClient.invalidateQueries({ queryKey: appointmentsKeys.all });
  };

  return {
    confirm: useMutation({
      mutationFn: (id: string) => appointmentsApi.confirm(id),
      onSuccess: refresh,
    }),
    reschedule: useMutation({
      mutationFn: (input: { id: string; startUtc: string }) =>
        appointmentsApi.reschedule(input.id, input.startUtc),
      // Both outcomes refresh: on failure the calendar has to go back to what
      // the server actually holds, not to what the drag suggested.
      onSettled: refresh,
    }),
    cancel: useMutation({
      mutationFn: (input: { id: string; reason: string }) =>
        appointmentsApi.cancel(input.id, input.reason),
      onSuccess: refresh,
    }),
  };
}

/**
 * Reads and writes the weekly opening hours.
 *
 * @returns The query and the save mutation.
 */
export function useAvailabilitySettings() {
  const queryClient = useQueryClient();

  const settings = useQuery<AvailabilitySettings>({
    queryKey: appointmentsKeys.settings(),
    queryFn: ({ signal }) => appointmentsApi.settings(signal),
  });

  const save = useMutation({
    mutationFn: (value: AvailabilitySettings) => appointmentsApi.saveSettings(value),
    onSuccess: (saved) => {
      // Everything is invalidated, not just the settings: changing the
      // opening hours changes what the public is offered.
      void queryClient.invalidateQueries({ queryKey: appointmentsKeys.all });
      queryClient.setQueryData(appointmentsKeys.settings(), saved);
    },
  });

  return { settings, save };
}
