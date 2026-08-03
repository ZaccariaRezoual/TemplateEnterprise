<script setup lang="ts">
/**
 * -----------------------------------------------------------------------------
 * AppointmentCalendar
 * -----------------------------------------------------------------------------
 *
 * The month/week/day calendar, with drag-to-move.
 *
 * **This is the only file in the application that knows FullCalendar exists.**
 * Everything above it speaks in appointments, dates and events of our own —
 * so replacing the library is this file, not a rewrite. That is the point of
 * the wrapper, and the reason it is worth having one at all.
 *
 * Responsibilities:
 * - Renders the appointments it is given.
 * - Reports two intentions upward: "show me this range" and "move this to
 *   there". It decides neither — the parent asks the server, and the server
 *   is free to refuse.
 * - Maps the library's own CSS variables onto our semantic tokens.
 *
 * On the theming: the library ships its own stylesheet, and that is exactly
 * how literal values climb back in after the design system has kept them out.
 * The `<style>` block below is the seam where its variables are bound to
 * ours, so the calendar follows light and dark like everything else and a
 * rebrand still means editing tokens.
 *
 * Only the free views are used — `dayGridMonth`, `timeGridWeek`,
 * `timeGridDay`. The resource/timeline views are the paid ones, and nothing
 * here reaches for them.
 */
import FullCalendar from "@fullcalendar/vue3";
import dayGridPlugin from "@fullcalendar/daygrid";
import interactionPlugin from "@fullcalendar/interaction";
import timeGridPlugin from "@fullcalendar/timegrid";
import type { CalendarOptions, EventClickArg, EventDropArg } from "@fullcalendar/core";
import { computed } from "vue";
import type { AdminAppointment } from "../api/appointments.api";

const props = defineProps<{
  /** Appointments to draw. */
  appointments: readonly AdminAppointment[];
  /** Whether the caller may move them; a read-only calendar is not draggable. */
  canEdit: boolean;
}>();

const emit = defineEmits<{
  /** The visible range changed; the parent fetches it. */
  rangeChange: [range: { fromUtc: string; toUtc: string }];
  /** An appointment was dragged. The parent asks the server, which may refuse. */
  move: [move: { id: string; startUtc: string; revert: () => void }];
  /** An appointment was clicked. */
  select: [id: string];
}>();

/**
 * How a status is coloured.
 *
 * Semantic tokens, resolved at runtime, never literals: the calendar has to
 * follow the theme like the rest of the interface. And colour is never alone
 * — the event title carries the state in words too, because a colour is not
 * information.
 */
const STATUS_COLOR: Record<string, string> = {
  Requested: "var(--color-warning)",
  Confirmed: "var(--color-primary)",
  Completed: "var(--color-text-muted)",
  Cancelled: "var(--color-border)",
  NoShow: "var(--color-danger)",
};

const events = computed(() =>
  props.appointments.map((appointment) => ({
    id: appointment.id,
    // The customer's name comes first: on a busy day that is what an operator
    // scans for.
    title: `${appointment.customerName} — ${appointment.serviceTitle}`,
    start: appointment.startUtc,
    end: appointment.endUtc,
    backgroundColor: STATUS_COLOR[appointment.status] ?? "var(--color-surface-sunken)",
    borderColor: appointment.hasConflict ? "var(--color-danger)" : "transparent",
    textColor: "var(--color-on-primary)",
    // Only a live appointment can be dragged; moving a cancelled one would
    // put back on the calendar something both sides consider over.
    editable:
      props.canEdit && (appointment.status === "Requested" || appointment.status === "Confirmed"),
    extendedProps: { hasConflict: appointment.hasConflict, status: appointment.status },
  })),
);

const options = computed<CalendarOptions>(() => ({
  plugins: [dayGridPlugin, timeGridPlugin, interactionPlugin],
  initialView: "timeGridWeek",
  headerToolbar: {
    left: "prev,next today",
    center: "title",
    right: "dayGridMonth,timeGridWeek,timeGridDay",
  },
  // Monday first and 24-hour time: the alternative is a calendar that reads
  // wrong to most of the world for no reason.
  firstDay: 1,
  locale: "it",
  slotLabelFormat: { hour: "2-digit", minute: "2-digit", hour12: false },
  eventTimeFormat: { hour: "2-digit", minute: "2-digit", hour12: false },
  allDaySlot: false,
  nowIndicator: true,
  height: "auto",
  events: events.value,
  editable: props.canEdit,
  eventStartEditable: props.canEdit,
  // Resizing would change the DURATION, which is what was sold. Moving picks
  // a time; it does not redefine the appointment.
  eventDurationEditable: false,
  datesSet: (info) => {
    emit("rangeChange", {
      fromUtc: info.start.toISOString(),
      toUtc: info.end.toISOString(),
    });
  },
  eventDrop: (info: EventDropArg) => {
    emit("move", {
      id: info.event.id,
      startUtc: info.event.start?.toISOString() ?? "",
      // Handed upward so the parent can put the event back when the server
      // refuses — without reloading the page, which would lose the view.
      revert: info.revert,
    });
  },
  eventClick: (info: EventClickArg) => emit("select", info.event.id),
}));
</script>

<template>
  <!-- The wrapper element is what the style block below targets: scoping the
       overrides here keeps the library's variables from leaking into the rest
       of the application. -->
  <div class="appointment-calendar">
    <FullCalendar :options="options" />
  </div>
</template>

<style>
/**
 * FullCalendar's variables, bound to ours.
 *
 * Not scoped: the library renders parts of its DOM outside this component's
 * subtree, and a scoped block would leave those unstyled. The class prefix is
 * what keeps it contained instead.
 *
 * Every value on the right-hand side is a semantic token. If the calendar
 * ever needs a colour that is not one, the answer is a new token — not a
 * hex code here.
 */
.appointment-calendar {
  --fc-page-bg-color: var(--color-surface);
  --fc-neutral-bg-color: var(--color-background);
  --fc-border-color: var(--color-border);
  --fc-today-bg-color: var(--color-background);

  --fc-button-bg-color: var(--color-surface);
  --fc-button-border-color: var(--color-border);
  --fc-button-text-color: var(--color-text);
  --fc-button-hover-bg-color: var(--color-background);
  --fc-button-hover-border-color: var(--color-border);
  --fc-button-active-bg-color: var(--color-primary);
  --fc-button-active-border-color: var(--color-primary);

  --fc-event-bg-color: var(--color-primary);
  --fc-event-border-color: var(--color-primary);
  --fc-event-text-color: var(--color-on-primary);

  --fc-now-indicator-color: var(--color-danger);

  color: var(--color-text);
}

/* The library's own focus outline is thinner than the accessibility contract
   allows; the ring token is the one that has been checked against WCAG. */
.appointment-calendar .fc-button:focus-visible,
.appointment-calendar .fc-event:focus-visible {
  outline: var(--focus-ring-width) solid var(--color-primary);
  outline-offset: var(--focus-ring-offset);
}
</style>
