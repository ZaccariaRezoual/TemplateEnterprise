<script setup lang="ts">
/**
 * -----------------------------------------------------------------------------
 * BookingPage
 * -----------------------------------------------------------------------------
 *
 * The public booking flow: pick a day, pick an hour, confirm.
 *
 * Responsibilities:
 * - Reads the availability of the service named in the route.
 * - Walks the visitor through three steps, one screen at a time.
 * - Handles the case that actually happens: the slot disappearing while the
 *   form is being filled in.
 *
 * **Three steps, not one form.** A single page with a month calendar, a list
 * of hours and a sign-in box is unreadable at 375px, and booking is something
 * people do from a phone.
 *
 * **The account is asked for LAST.** Choosing comes first because asking for
 * a password before showing anything is what makes people give up — and the
 * chosen slot survives the sign-in, because it lives in this component's
 * state and the visitor comes back to the same page.
 *
 * The word used throughout is "request", never "booking": until an
 * administrator confirms, that hour belongs to nobody. Saying otherwise would
 * be a promise the business has not made.
 */
import { Button, Card, Input, PageSection, Skeleton, Textarea } from "@enterprise/ui";
import { BusinessError, UnauthorizedError } from "@enterprise/shared";
import { computed, ref, watch } from "vue";
import { RouterLink, useRoute } from "vue-router";
import { useAvailability, useRequestAppointment } from "../composables/useAppointments";
import { useBookingSession } from "../session";
import type { Slot } from "../api/appointments.api";

const route = useRoute();

// Asked of the HOST, never of the Auth module: this page must keep working
// in an application assembled without authentication.
const session = useBookingSession();

const slug = computed(() => (typeof route.params["slug"] === "string" ? route.params["slug"] : ""));

/**
 * The service is addressed by SLUG in the URL but by id in the API, and the
 * host resolves one into the other — this module does not know the Services
 * module. Until it is resolved the availability query stays disabled.
 */
const serviceId = computed(() =>
  typeof route.query["serviceId"] === "string" ? route.query["serviceId"] : "",
);

/** Which of the three screens is on. */
const step = ref<"day" | "hour" | "confirm">("day");

const today = new Date();

/** First and last local dates asked for: four weeks is what a month view shows. */
const from = ref(toLocalDate(today));
const to = ref(toLocalDate(new Date(today.getTime() + 27 * 24 * 60 * 60 * 1000)));

const availability = useAvailability(serviceId, from, to);
const request = useRequestAppointment();

const selectedDate = ref<string | undefined>(undefined);
const selectedSlot = ref<Slot | undefined>(undefined);
const contactPhone = ref("");
const customerNote = ref("");
const formError = ref<string | undefined>(undefined);
const requested = ref(false);

/** Only the days that actually have something to offer are clickable. */
const openDays = computed(
  () => availability.data.value?.days.filter((day) => day.slots.length > 0) ?? [],
);

const slotsOfSelectedDay = computed(
  () => availability.data.value?.days.find((day) => day.date === selectedDate.value)?.slots ?? [],
);

/** "yyyy-MM-dd" in the BROWSER's local date, which is what a date input means. */
function toLocalDate(value: Date): string {
  return `${value.getFullYear()}-${String(value.getMonth() + 1).padStart(2, "0")}-${String(value.getDate()).padStart(2, "0")}`;
}

/**
 * Renders an instant in the BUSINESS time zone.
 *
 * Not the visitor's: the appointment happens where the business is, and an
 * hour silently converted into the reader's own zone is how somebody turns up
 * at the wrong time.
 */
function localTime(instant: string): string {
  return new Intl.DateTimeFormat("it-IT", {
    hour: "2-digit",
    minute: "2-digit",
    timeZone: availability.data.value?.timeZoneId,
  }).format(new Date(instant));
}

function localDay(date: string): string {
  return new Intl.DateTimeFormat("it-IT", {
    weekday: "long",
    day: "numeric",
    month: "long",
  }).format(new Date(`${date}T12:00:00`));
}

function chooseDay(date: string): void {
  selectedDate.value = date;
  selectedSlot.value = undefined;
  step.value = "hour";
}

function chooseSlot(slot: Slot): void {
  selectedSlot.value = slot;
  step.value = "confirm";
}

// The chosen hour can stop existing while the visitor is still typing. When
// the refreshed availability no longer contains it, they are sent back to the
// hours with an explanation — not shown a generic error on submit.
watch(
  () => availability.data.value,
  () => {
    if (selectedSlot.value === undefined || step.value !== "confirm") {
      return;
    }

    const stillThere = slotsOfSelectedDay.value.some(
      (slot) => slot.startUtc === selectedSlot.value?.startUtc,
    );

    if (!stillThere) {
      selectedSlot.value = undefined;
      step.value = "hour";
      formError.value = "Quell'orario è appena stato preso. Scegline un altro.";
    }
  },
);

async function submit(): Promise<void> {
  formError.value = undefined;

  if (selectedSlot.value === undefined) {
    return;
  }

  if (contactPhone.value.trim() === "") {
    formError.value =
      "Lascia un numero di telefono: è così che ti raggiungiamo se qualcosa cambia.";
    return;
  }

  try {
    await request.mutateAsync({
      serviceId: serviceId.value,
      startUtc: selectedSlot.value.startUtc,
      contactPhone: contactPhone.value.trim(),
      customerNote: customerNote.value.trim() === "" ? undefined : customerNote.value.trim(),
    });
    requested.value = true;
  } catch (error) {
    if (error instanceof BusinessError) {
      // The server recomputed and disagreed. Back to the hours, with the
      // reason: a generic failure would leave the visitor pressing the same
      // button again.
      selectedSlot.value = undefined;
      step.value = "hour";
      formError.value = "Quell'orario non è più disponibile. Scegline un altro.";
      void availability.refetch();
    } else if (error instanceof UnauthorizedError) {
      formError.value = "Accedi per completare la richiesta.";
    } else {
      formError.value = "Non siamo riusciti a inviare la richiesta. Riprova fra poco.";
    }
  }
}
</script>

<template>
  <PageSection
    :title="requested ? 'Richiesta inviata' : (availability.data.value?.serviceTitle ?? 'Prenota')"
    heading-level="h1"
  >
    <!-- The outcome REPLACES the form rather than clearing it: an empty form
         after submitting reads as "nothing happened", and people send twice. -->
    <template v-if="requested">
      <p class="max-w-prose text-text-muted">
        Abbiamo ricevuto la tua richiesta. Non è ancora una conferma: ti scriviamo appena
        l'appuntamento è confermato.
      </p>
      <RouterLink v-slot="{ href, navigate }" to="/admin/my-appointments" custom>
        <Button :href="href" class="mt-6" @click="navigate">I miei appuntamenti</Button>
      </RouterLink>
    </template>

    <template v-else-if="serviceId === ''">
      <p role="alert" class="max-w-prose text-danger">
        Manca il servizio da prenotare. Torna alla pagina del servizio e riprova.
      </p>
      <RouterLink v-slot="{ href, navigate }" :to="`/services/${slug}`" custom>
        <Button :href="href" variant="secondary" class="mt-6" @click="navigate">
          Torna al servizio
        </Button>
      </RouterLink>
    </template>

    <template v-else>
      <!-- Three dots, not a progress bar: the visitor needs to know how many
           steps are left, and three is small enough to just show. -->
      <ol class="mb-8 flex items-center gap-2 text-sm text-text-muted" aria-label="Passi">
        <li :class="step === 'day' ? 'font-medium text-text' : ''">1. Giorno</li>
        <li aria-hidden="true">·</li>
        <li :class="step === 'hour' ? 'font-medium text-text' : ''">2. Ora</li>
        <li aria-hidden="true">·</li>
        <li :class="step === 'confirm' ? 'font-medium text-text' : ''">3. Conferma</li>
      </ol>

      <p v-if="formError" role="alert" class="mb-6 max-w-prose text-danger">{{ formError }}</p>

      <div v-if="availability.isPending.value" class="space-y-3">
        <Skeleton class="h-10 w-full max-w-md" />
        <Skeleton class="h-10 w-full max-w-md" />
      </div>

      <p v-else-if="availability.isError.value" role="alert" class="max-w-prose text-danger">
        Non siamo riusciti a caricare le disponibilità. Riprova fra poco.
      </p>

      <!-- STEP 1 — the day -->
      <template v-else-if="step === 'day'">
        <p v-if="openDays.length === 0" class="max-w-prose text-text-muted">
          Non ci sono orari disponibili nelle prossime settimane. Riprova più avanti o contattaci.
        </p>
        <ul v-else class="grid grid-cols-1 gap-2 sm:grid-cols-2 lg:grid-cols-3">
          <li v-for="day in openDays" :key="day.date">
            <!-- Full-width targets: this is booked from a phone, with a
                 thumb. The 44px floor comes from the tokens. -->
            <Button variant="secondary" block @click="chooseDay(day.date)">
              {{ localDay(day.date) }}
            </Button>
          </li>
        </ul>
      </template>

      <!-- STEP 2 — the hour -->
      <template v-else-if="step === 'hour'">
        <p class="mb-4 text-text-muted">{{ localDay(selectedDate ?? "") }}</p>
        <ul class="grid grid-cols-2 gap-2 sm:grid-cols-4">
          <li v-for="slot in slotsOfSelectedDay" :key="slot.startUtc">
            <Button variant="secondary" block @click="chooseSlot(slot)">
              {{ localTime(slot.startUtc) }}
            </Button>
          </li>
        </ul>
        <Button variant="ghost" class="mt-6" @click="step = 'day'">Cambia giorno</Button>
      </template>

      <!-- STEP 3 — confirm -->
      <template v-else>
        <Card title="Riepilogo" heading-level="h2">
          <dl class="grid grid-cols-2 gap-3">
            <dt class="text-sm text-text-muted">Servizio</dt>
            <dd class="font-medium">{{ availability.data.value?.serviceTitle }}</dd>
            <dt class="text-sm text-text-muted">Quando</dt>
            <dd class="font-medium tabular-nums">
              {{ localDay(selectedDate ?? "") }},
              {{ localTime(selectedSlot?.startUtc ?? "") }}
            </dd>
            <dt class="text-sm text-text-muted">Durata</dt>
            <dd class="font-medium tabular-nums">
              {{ availability.data.value?.durationMinutes }} min
            </dd>
          </dl>
        </Card>

        <!-- The account is asked for here and nowhere earlier. -->
        <template v-if="!session.isAuthenticated()">
          <p class="mt-6 max-w-prose text-text-muted">
            Per completare serve un account: ci serve un recapito per avvisarti se qualcosa cambia.
          </p>
          <RouterLink v-slot="{ href, navigate }" :to="session.signInPath()" custom>
            <Button :href="href" class="mt-4" @click="navigate">Accedi o registrati</Button>
          </RouterLink>
        </template>

        <form v-else class="mt-6 max-w-md space-y-4" @submit.prevent="submit">
          <Input
            v-model="contactPhone"
            label="Telefono"
            type="tel"
            required
            hint="Lo usiamo solo per questo appuntamento."
          />
          <Textarea v-model="customerNote" label="Nota (facoltativa)" :maxlength="2000" :rows="3" />
          <div class="flex flex-wrap items-center gap-3">
            <Button type="submit" :loading="request.isPending.value">Invia richiesta</Button>
            <Button variant="ghost" @click="step = 'hour'">Cambia ora</Button>
          </div>
        </form>
      </template>
    </template>
  </PageSection>
</template>
