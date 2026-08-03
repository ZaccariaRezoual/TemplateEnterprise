<script setup lang="ts">
/**
 * -----------------------------------------------------------------------------
 * AddToCalendar
 * -----------------------------------------------------------------------------
 *
 * "Put this in my calendar" — Google, Outlook, or a file for everything else.
 *
 * Used from both sides: the customer's own list and the administrator's
 * calendar panel. One component, because the question is identical and the
 * two answers must not drift.
 *
 * Responsibilities:
 * - Builds the Google and Outlook template URLs from the appointment.
 * - Links to the server's `.ics`, which covers Apple Calendar, Thunderbird,
 *   and Outlook desktop.
 *
 * **No OAuth, no tokens, no consent screen.** These are plain links: the
 * person lands on their own calendar with the event pre-filled and decides
 * whether to save it. Writing into their calendar through an API would need
 * an authorization almost nobody grants a supplier, to do something a link
 * already does in one click.
 *
 * The `.ics` is fetched from the SERVER rather than assembled here, and that
 * is the important part: it carries the `UID` and `SEQUENCE` that make a
 * later download REPLACE the entry instead of adding a second one. Building
 * it in the browser would mean a second implementation of the format, and the
 * two would drift on exactly that detail.
 */
import { computed } from "vue";

const props = defineProps<{
  /** Identifier, used to fetch the .ics from the API. */
  id: string;
  /** What the entry is called in the person's calendar. */
  title: string;
  /** Start instant, ISO 8601 UTC. */
  startUtc: string;
  /** End instant, ISO 8601 UTC. */
  endUtc: string;
  /**
   * Origin of the API, for the `.ics` link. Empty for a same-origin
   * deployment, which is the normal case.
   */
  apiOrigin?: string | undefined;
}>();

/** "20260302T090000Z" — the compact UTC form both providers expect. */
function stamp(instant: string): string {
  return new Date(instant)
    .toISOString()
    .replace(/[-:]/g, "")
    .replace(/\.\d{3}/, "");
}

const googleUrl = computed(
  () =>
    "https://calendar.google.com/calendar/render?action=TEMPLATE" +
    `&text=${encodeURIComponent(props.title)}` +
    `&dates=${stamp(props.startUtc)}/${stamp(props.endUtc)}`,
);

const outlookUrl = computed(
  () =>
    // The consumer endpoint. Microsoft 365 accounts are served by
    // outlook.office.com with the same query shape; outlook.live.com
    // redirects a work account there rather than failing, so one link covers
    // both instead of asking the person which kind of account they have.
    "https://outlook.live.com/calendar/0/deeplink/compose?path=/calendar/action/compose" +
    "&rru=addevent" +
    `&subject=${encodeURIComponent(props.title)}` +
    `&startdt=${encodeURIComponent(props.startUtc)}` +
    `&enddt=${encodeURIComponent(props.endUtc)}`,
);

const icsUrl = computed(() => `${props.apiOrigin ?? ""}/api/appointments/${props.id}/calendar.ics`);
</script>

<template>
  <div class="flex flex-wrap items-center gap-x-4 gap-y-2">
    <span class="text-sm text-text-muted">Aggiungi al calendario:</span>

    <!-- Real links, not buttons: they open elsewhere, so middle-click and
         "open in new tab" have to keep working. `noopener` because the target
         is a third-party origin. -->
    <a
      :href="googleUrl"
      target="_blank"
      rel="noopener noreferrer"
      class="text-sm font-medium text-primary underline-offset-2 hover:underline"
    >
      Google
    </a>
    <a
      :href="outlookUrl"
      target="_blank"
      rel="noopener noreferrer"
      class="text-sm font-medium text-primary underline-offset-2 hover:underline"
    >
      Outlook
    </a>
    <!-- Not `target="_blank"`: this one downloads rather than navigating, and
         a blank tab that immediately closes itself is disconcerting. -->
    <a
      :href="icsUrl"
      class="text-sm font-medium text-primary underline-offset-2 hover:underline"
      data-testid="add-to-calendar-ics"
    >
      Apple, Outlook desktop, altri (.ics)
    </a>
  </div>
</template>
