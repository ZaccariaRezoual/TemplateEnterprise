<script setup lang="ts">
/**
 * -----------------------------------------------------------------------------
 * AvailabilitySettingsPage
 * -----------------------------------------------------------------------------
 *
 * The weekly opening hours, and the days that depart from them.
 *
 * Responsibilities:
 * - Edits the whole schedule as ONE document and saves it in one call.
 * - Shows which time zone the times are local to.
 *
 * Saved whole, not row by row, because that is how it is thought about:
 * "Tuesdays move to the afternoon" is one decision, and three requests that
 * can half-apply would leave a schedule nobody intended.
 *
 * The times are LOCAL to the business time zone, and the page says so out
 * loud — an operator in another country editing "09:00" needs to know whose
 * nine o'clock it is.
 */
import { Button, Card, Checkbox, Input } from "@enterprise/ui";
import { usePermissions } from "@enterprise/module-authorization";
import { ref, watch } from "vue";
import { useAvailabilitySettings } from "../composables/useAppointments";
import type { AvailabilitySettings } from "../api/appointments.api";

const { can } = usePermissions();
const { settings, save } = useAvailabilitySettings();

/** Monday first: it is the working week, and the calendar starts there too. */
const DAYS = [
  { value: "Monday", label: "Lunedì" },
  { value: "Tuesday", label: "Martedì" },
  { value: "Wednesday", label: "Mercoledì" },
  { value: "Thursday", label: "Giovedì" },
  { value: "Friday", label: "Venerdì" },
  { value: "Saturday", label: "Sabato" },
  { value: "Sunday", label: "Domenica" },
] as const;

/** One editable row per day: open or shut, and the hours when open. */
interface DayRow {
  open: boolean;
  startLocal: string;
  endLocal: string;
}

const rows = ref<Record<string, DayRow>>(
  Object.fromEntries(
    DAYS.map((day) => [day.value, { open: false, startLocal: "09:00", endLocal: "18:00" }]),
  ),
);

const overrides = ref<AvailabilitySettings["exceptions"]>([]);
const formError = ref<string | undefined>(undefined);

// Fills the form once the configuration arrives.
watch(
  () => settings.data.value,
  (loaded) => {
    if (loaded === undefined) {
      return;
    }

    for (const day of DAYS) {
      const rule = loaded.rules.find((entry) => entry.dayOfWeek === day.value);
      rows.value[day.value] = {
        open: rule !== undefined,
        startLocal: rule?.startLocal ?? "09:00",
        endLocal: rule?.endLocal ?? "18:00",
      };
    }

    overrides.value = [...loaded.exceptions];
  },
  { immediate: true },
);

function addClosure(): void {
  overrides.value = [
    ...overrides.value,
    { dateLocal: "", isClosed: true, startLocal: null, endLocal: null, reason: "" },
  ];
}

function removeOverride(index: number): void {
  overrides.value = overrides.value.filter((_, position) => position !== index);
}

async function submit(): Promise<void> {
  formError.value = undefined;

  const rules = DAYS.filter((day) => rows.value[day.value]?.open).map((day) => ({
    dayOfWeek: day.value,
    startLocal: rows.value[day.value]!.startLocal,
    endLocal: rows.value[day.value]!.endLocal,
  }));

  if (rules.some((rule) => rule.endLocal <= rule.startLocal)) {
    // Compared as strings on purpose: "HH:mm" sorts correctly as text, and
    // the server checks it again anyway.
    formError.value = "L'orario di chiusura deve essere successivo a quello di apertura.";
    return;
  }

  if (overrides.value.some((entry) => entry.dateLocal === "" || entry.reason.trim() === "")) {
    formError.value = "Ogni eccezione ha bisogno di una data e di un motivo.";
    return;
  }

  try {
    await save.mutateAsync({
      timeZoneId: settings.data.value?.timeZoneId ?? "",
      rules: rules as AvailabilitySettings["rules"],
      exceptions: overrides.value,
    });
  } catch (error) {
    formError.value = (error as Error).message;
  }
}
</script>

<template>
  <div class="space-y-6">
    <header>
      <h1 class="text-display font-semibold tracking-tight">Orari di apertura</h1>
      <p class="mt-1 text-text-muted">
        Gli orari sono locali al fuso dell'attività
        <span class="font-medium">{{ settings.data.value?.timeZoneId ?? "…" }}</span
        >. Cambiarli non tocca gli appuntamenti già presi.
      </p>
    </header>

    <!-- Loading is checked FIRST, before the permission. The permissions
         arrive asynchronously after sign-in, so asking `can()` too early
         answers "no" for everybody — and telling someone they lack a
         permission they actually hold is worse than making them wait a
         moment. Same reason an empty table must not mean "no access". -->
    <p v-if="settings.isPending.value" class="text-sm text-text-muted">Caricamento…</p>

    <p v-else-if="!can('appointments.write')" role="alert" class="text-sm text-danger">
      Non hai il permesso di modificare gli orari.
    </p>

    <template v-else>
      <form class="space-y-6" @submit.prevent="submit">
        <Card title="Settimana tipo" heading-level="h2">
          <ul class="space-y-4">
            <li
              v-for="day in DAYS"
              :key="day.value"
              class="flex flex-col gap-3 border-b border-border pb-4 last:border-0 last:pb-0 sm:flex-row sm:items-end"
            >
              <div class="sm:w-48">
                <Checkbox v-model="rows[day.value]!.open" :label="day.label" />
              </div>
              <div v-if="rows[day.value]!.open" class="flex flex-1 flex-wrap gap-3">
                <Input
                  v-model="rows[day.value]!.startLocal"
                  label="Apre"
                  class="sm:max-w-32"
                  placeholder="09:00"
                />
                <Input
                  v-model="rows[day.value]!.endLocal"
                  label="Chiude"
                  class="sm:max-w-32"
                  placeholder="18:00"
                />
              </div>
              <p v-else class="flex-1 text-sm text-text-muted">Chiuso</p>
            </li>
          </ul>
        </Card>

        <Card title="Chiusure e aperture straordinarie" heading-level="h2">
          <p v-if="overrides.length === 0" class="text-sm text-text-muted">
            Nessuna eccezione. Una chiusura vince sempre sulla settimana tipo; un'apertura
            straordinaria la sostituisce per quel giorno.
          </p>

          <ul v-else class="space-y-4">
            <li
              v-for="(entry, index) in overrides"
              :key="index"
              class="flex flex-col gap-3 border-b border-border pb-4 last:border-0 last:pb-0 sm:flex-row sm:items-end"
            >
              <Input
                v-model="entry.dateLocal"
                label="Data"
                class="sm:max-w-44"
                placeholder="2026-12-25"
              />
              <Input
                v-model="entry.reason"
                label="Motivo"
                hint="Solo per voi: il visitatore vede un giorno senza orari."
              />
              <Button variant="ghost" size="sm" @click="removeOverride(index)">Rimuovi</Button>
            </li>
          </ul>

          <Button variant="secondary" size="sm" class="mt-4" @click="addClosure">
            Aggiungi una chiusura
          </Button>
        </Card>

        <div class="flex flex-wrap items-center gap-3">
          <Button type="submit" :loading="save.isPending.value">Salva</Button>
          <p v-if="formError" role="alert" class="text-sm text-danger">{{ formError }}</p>
          <p v-else-if="save.isSuccess.value" role="status" class="text-sm text-success">
            Orari salvati.
          </p>
        </div>
      </form>
    </template>
  </div>
</template>
