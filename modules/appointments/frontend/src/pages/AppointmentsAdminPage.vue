<script setup lang="ts">
/**
 * -----------------------------------------------------------------------------
 * AppointmentsAdminPage
 * -----------------------------------------------------------------------------
 *
 * The working calendar: everything booked, and everything you can do to it.
 *
 * Responsibilities:
 * - Feeds the calendar the window it asks for.
 * - Opens a detail panel for the selected appointment, with confirm, move and
 *   cancel.
 * - Puts a dragged appointment BACK when the server refuses the move.
 *
 * The last one is the behaviour worth naming. A drag is an intention: the
 * calendar was drawn minutes ago and the database may disagree. When it does,
 * the event returns to where it was and the reason is shown — no reload,
 * because a reload would lose the week the operator was looking at.
 *
 * Requests that overlap a confirmed appointment arrive already flagged by the
 * server (`hasConflict`). They are shown as such rather than left to be
 * discovered by clicking confirm and being refused.
 */
import { Badge, Button, Card, Textarea } from "@enterprise/ui";
import { usePermissions } from "@enterprise/module-authorization";
import { computed, ref } from "vue";
import AddToCalendar from "../components/AddToCalendar.vue";
import AppointmentCalendar from "../components/AppointmentCalendar.vue";
import { useAppointmentActions, useCalendar } from "../composables/useAppointments";
import { appointmentsApiOrigin } from "../api/appointments.api";
import type { AdminAppointment } from "../api/appointments.api";

const { can } = usePermissions();
const apiOrigin = appointmentsApiOrigin();

const fromUtc = ref("");
const toUtc = ref("");
const calendar = useCalendar(fromUtc, toUtc);
const { confirm, reschedule, cancel } = useAppointmentActions();

const selectedId = ref<string | undefined>(undefined);
const cancelReason = ref("");
const actionError = ref<string | undefined>(undefined);

const selected = computed<AdminAppointment | undefined>(() =>
  calendar.data.value?.find((appointment) => appointment.id === selectedId.value),
);

const canEdit = computed(() => can("appointments.write"));

function onRangeChange(range: { fromUtc: string; toUtc: string }): void {
  fromUtc.value = range.fromUtc;
  toUtc.value = range.toUtc;
}

async function onMove(move: { id: string; startUtc: string; revert: () => void }): Promise<void> {
  actionError.value = undefined;

  try {
    await reschedule.mutateAsync({ id: move.id, startUtc: move.startUtc });
  } catch (error) {
    // The server said no. Putting the event back is the honest response: the
    // calendar must show what is true, not what was attempted.
    move.revert();
    actionError.value = (error as Error).message;
  }
}

async function onConfirm(id: string): Promise<void> {
  actionError.value = undefined;
  try {
    await confirm.mutateAsync(id);
  } catch (error) {
    actionError.value = (error as Error).message;
  }
}

async function onCancel(id: string): Promise<void> {
  actionError.value = undefined;

  if (cancelReason.value.trim() === "") {
    actionError.value = "Scrivi un motivo: è quello che legge il cliente.";
    return;
  }

  try {
    await cancel.mutateAsync({ id, reason: cancelReason.value.trim() });
    cancelReason.value = "";
    selectedId.value = undefined;
  } catch (error) {
    actionError.value = (error as Error).message;
  }
}

function when(appointment: AdminAppointment): string {
  return new Intl.DateTimeFormat("it-IT", {
    weekday: "long",
    day: "numeric",
    month: "long",
    hour: "2-digit",
    minute: "2-digit",
  }).format(new Date(appointment.startUtc));
}
</script>

<template>
  <div class="space-y-6">
    <header class="flex flex-wrap items-start justify-between gap-4">
      <div>
        <h1 class="text-display font-semibold tracking-tight">Appuntamenti</h1>
        <p class="mt-1 text-text-muted">
          Trascina per spostare. Il server rivalida sempre: se l'orario è occupato, torna indietro.
        </p>
      </div>
      <RouterLink v-slot="{ href, navigate }" to="/admin/appointments/availability" custom>
        <Button :href="href" variant="secondary" @click="navigate">Orari di apertura</Button>
      </RouterLink>
    </header>

    <p v-if="!can('appointments.read')" role="alert" class="text-sm text-danger">
      Non hai il permesso di vedere il calendario.
    </p>

    <template v-else>
      <p v-if="actionError" role="alert" class="text-sm text-danger">{{ actionError }}</p>

      <Card>
        <AppointmentCalendar
          :appointments="calendar.data.value ?? []"
          :can-edit="canEdit"
          @range-change="onRangeChange"
          @move="onMove"
          @select="selectedId = $event"
        />
      </Card>

      <Card v-if="selected" :title="selected.serviceTitle" heading-level="h2">
        <dl class="grid grid-cols-1 gap-3 sm:grid-cols-2">
          <div>
            <dt class="text-sm text-text-muted">Quando</dt>
            <dd class="font-medium tabular-nums">{{ when(selected) }}</dd>
          </div>
          <div>
            <dt class="text-sm text-text-muted">Stato</dt>
            <dd class="mt-1 flex flex-wrap items-center gap-2">
              <Badge :variant="selected.status === 'Confirmed' ? 'success' : 'info'">
                {{ selected.status }}
              </Badge>
              <!-- Flagged by the server: confirming it would be refused by
                   the database, and finding that out by clicking is a poor
                   way to learn it. -->
              <Badge v-if="selected.hasConflict" variant="danger">
                Si sovrappone a un appuntamento confermato
              </Badge>
            </dd>
          </div>
          <div>
            <dt class="text-sm text-text-muted">Cliente</dt>
            <dd class="font-medium">{{ selected.customerName }}</dd>
          </div>
          <div>
            <dt class="text-sm text-text-muted">Telefono</dt>
            <dd class="font-medium tabular-nums">{{ selected.contactPhone }}</dd>
          </div>
          <div v-if="selected.customerNote" class="sm:col-span-2">
            <dt class="text-sm text-text-muted">Nota del cliente</dt>
            <dd class="max-w-prose">{{ selected.customerNote }}</dd>
          </div>
        </dl>

        <!-- The operator needs it in their own calendar too, not just in
             the one on screen. Confirmed only, for the same reason as the
             customer's list. -->
        <AddToCalendar
          v-if="selected.status === 'Confirmed'"
          :id="selected.id"
          class="mt-6 border-t border-border pt-6"
          :title="`${selected.customerName} — ${selected.serviceTitle}`"
          :start-utc="selected.startUtc"
          :end-utc="selected.endUtc"
          :api-origin="apiOrigin"
        />

        <div v-if="canEdit" class="mt-6 space-y-4 border-t border-border pt-6">
          <Button
            v-if="selected.status === 'Requested'"
            :loading="confirm.isPending.value"
            @click="onConfirm(selected.id)"
          >
            Conferma
          </Button>

          <div v-if="selected.status === 'Requested' || selected.status === 'Confirmed'">
            <Textarea
              v-model="cancelReason"
              label="Motivo dell'annullamento"
              :rows="2"
              hint="Lo legge il cliente: «annullato» senza spiegazione è il messaggio che fa smettere di prenotare."
            />
            <Button
              variant="danger"
              class="mt-3"
              :loading="cancel.isPending.value"
              @click="onCancel(selected.id)"
            >
              Annulla appuntamento
            </Button>
          </div>
        </div>
      </Card>
    </template>
  </div>
</template>
