<script setup lang="ts">
/**
 * -----------------------------------------------------------------------------
 * MyAppointmentsPage
 * -----------------------------------------------------------------------------
 *
 * What the signed-in person has booked.
 *
 * Responsibilities:
 * - Lists the caller's own appointments, upcoming by default.
 * - Lets them call one off, when the server says they still may.
 * - Distinguishes a REQUEST from a confirmed appointment, in words.
 *
 * It lives under `/admin` because that is where the authenticated area is,
 * but it carries no permission: these are the person's own data, and a
 * permission would be the wrong tool for "is this mine?". The endpoint reads
 * the caller from the token, so there is no identifier to tamper with.
 *
 * `canCancel` comes from the SERVER. It decides what is rendered; the API
 * checks it again on the way in, because only one of the two is a boundary.
 */
import { Badge, Button, Card, Checkbox } from "@enterprise/ui";
import { ref } from "vue";
import AddToCalendar from "../components/AddToCalendar.vue";
import { useCancelMyAppointment, useMyAppointments } from "../composables/useAppointments";
import { appointmentsApiOrigin } from "../api/appointments.api";
import type { AppointmentStatus, MyAppointment } from "../api/appointments.api";

const apiOrigin = appointmentsApiOrigin();

const includePast = ref(false);
const appointments = useMyAppointments(includePast);
const cancel = useCancelMyAppointment();

/** Which appointment the confirmation row is open for, if any. */
const pendingCancel = ref<string | undefined>(undefined);

/**
 * How a status reads to the person who booked.
 *
 * "Requested" becomes "waiting for confirmation" rather than a bare label:
 * the difference between a wait and a promise is the whole point of the
 * state, and a one-word badge does not carry it.
 */
const STATUS_TEXT: Record<
  AppointmentStatus,
  { label: string; variant: "neutral" | "success" | "warning" | "info" }
> = {
  Requested: { label: "In attesa di conferma", variant: "info" },
  Confirmed: { label: "Confermato", variant: "success" },
  Completed: { label: "Concluso", variant: "neutral" },
  Cancelled: { label: "Annullato", variant: "warning" },
  NoShow: { label: "Non presentato", variant: "warning" },
};

function when(appointment: MyAppointment): string {
  return new Intl.DateTimeFormat("it-IT", {
    weekday: "long",
    day: "numeric",
    month: "long",
    hour: "2-digit",
    minute: "2-digit",
  }).format(new Date(appointment.startUtc));
}

async function confirmCancel(id: string): Promise<void> {
  await cancel.mutateAsync({ id });
  pendingCancel.value = undefined;
}
</script>

<template>
  <div class="space-y-6">
    <header>
      <h1 class="text-display font-semibold tracking-tight">I miei appuntamenti</h1>
      <p class="mt-1 text-text-muted">Quello che hai prenotato, e a che punto è.</p>
    </header>

    <Checkbox v-model="includePast" label="Mostra anche quelli passati" />

    <p v-if="appointments.isPending.value" class="text-sm text-text-muted">Caricamento…</p>

    <p v-else-if="appointments.isError.value" role="alert" class="text-sm text-danger">
      {{ appointments.error.value?.message }}
    </p>

    <p v-else-if="appointments.data.value?.length === 0" class="text-text-muted">
      Non hai appuntamenti. Scegli un servizio per prenotarne uno.
    </p>

    <ul v-else class="space-y-4">
      <li v-for="appointment in appointments.data.value" :key="appointment.id">
        <Card>
          <div class="flex flex-wrap items-start justify-between gap-4">
            <div>
              <h2 class="text-title font-semibold">{{ appointment.serviceTitle }}</h2>
              <p class="mt-1 text-text-muted tabular-nums">{{ when(appointment) }}</p>

              <!-- The reason is shown because the customer is the one who
                   needs it: "cancelled" with no explanation is the message
                   that makes someone stop booking. -->
              <p
                v-if="appointment.cancellationReason"
                class="mt-2 max-w-prose text-sm text-text-muted"
              >
                {{ appointment.cancellationReason }}
              </p>
            </div>

            <Badge :variant="STATUS_TEXT[appointment.status].variant">
              {{ STATUS_TEXT[appointment.status].label }}
            </Badge>
          </div>

          <!-- Only once it is CONFIRMED. Putting a request in someone's
               calendar would make a promise the business has not made, and
               they would have to remember to take it out again. -->
          <AddToCalendar
            v-if="appointment.status === 'Confirmed'"
            :id="appointment.id"
            class="mt-4 border-t border-border pt-4"
            :title="appointment.serviceTitle"
            :start-utc="appointment.startUtc"
            :end-utc="appointment.endUtc"
            :api-origin="apiOrigin"
          />

          <div v-if="appointment.canCancel" class="mt-4 flex flex-wrap items-center gap-2">
            <Button
              v-if="pendingCancel !== appointment.id"
              variant="ghost"
              size="sm"
              @click="pendingCancel = appointment.id"
            >
              Annulla
            </Button>
            <template v-else>
              <Button
                variant="danger"
                size="sm"
                :loading="cancel.isPending.value"
                @click="confirmCancel(appointment.id)"
              >
                Confermi?
              </Button>
              <Button variant="ghost" size="sm" @click="pendingCancel = undefined">
                Lascia stare
              </Button>
            </template>
          </div>
        </Card>
      </li>
    </ul>

    <p v-if="cancel.isError.value" role="alert" class="text-sm text-danger">
      {{ cancel.error.value?.message }}
    </p>
  </div>
</template>
