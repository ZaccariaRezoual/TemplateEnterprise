<script setup lang="ts">
/**
 * -----------------------------------------------------------------------------
 * ServiceEditPage
 * -----------------------------------------------------------------------------
 *
 * The form that creates and edits a service. One component for both, because
 * they are the same form — two would be two validators that must agree.
 *
 * Responsibilities:
 * - Loads the service when the route carries an id, and starts empty
 *   otherwise.
 * - Validates client-side for fast feedback, then saves through the
 *   composable. The server revalidates everything.
 * - Pre-fills the slug from the title while the address has never been
 *   published, and WARNS instead of forbidding when changing one that has.
 * - Shows the gallery only once the service exists: an image needs something
 *   to belong to.
 *
 * The business logic lives in the composables; this component decides what is
 * on screen and when.
 */
import { Button, Card, Checkbox, Input, Textarea } from "@enterprise/ui";
import { ValidationError } from "@enterprise/shared";
import { computed, ref, watch } from "vue";
import { useRoute, useRouter } from "vue-router";
import ServiceGallery from "../components/ServiceGallery.vue";
import { useSaveService, useService } from "../composables/useServices";
import { serviceFormSchema } from "../validators/service.validator";

const route = useRoute();
const router = useRouter();

/** Empty while creating: the same form, without a subject yet. */
const serviceId = computed(() =>
  typeof route.params["id"] === "string" ? route.params["id"] : "",
);
const isNew = computed(() => serviceId.value === "");

const service = useService(serviceId);
const save = useSaveService();

const title = ref("");
const slug = ref("");
const shortDescription = ref("");
const description = ref("");
const durationMinutes = ref("");
const price = ref("");
const currency = ref("EUR");
const isPublished = ref(false);
const isBookable = ref(false);
const sortOrder = ref("0");

/** The slug as it was loaded, to tell an edit apart from a pre-fill. */
const originalSlug = ref("");
/** Whether the loaded service was already visible to the public. */
const wasPublished = ref(false);

const fieldErrors = ref<Record<string, string>>({});
const formError = ref<string | undefined>(undefined);

// Fills the form once the service arrives. `immediate` is not needed: while
// creating there is nothing to load, and the refs already hold the defaults.
watch(
  () => service.data.value,
  (loaded) => {
    if (loaded === undefined) {
      return;
    }

    title.value = loaded.title;
    slug.value = loaded.slug;
    originalSlug.value = loaded.slug;
    wasPublished.value = loaded.isPublished;
    shortDescription.value = loaded.shortDescription;
    description.value = loaded.description;
    durationMinutes.value = loaded.durationMinutes?.toString() ?? "";
    price.value = loaded.price?.toString() ?? "";
    currency.value = loaded.currency ?? "EUR";
    isPublished.value = loaded.isPublished;
    isBookable.value = loaded.isBookable;
    sortOrder.value = loaded.sortOrder.toString();
  },
  { immediate: true },
);

/**
 * Turns a title into the address the server would derive from it.
 *
 * Kept in step with `Slug.From` on the backend, which remains the authority:
 * this is a live preview so the administrator sees the address before saving,
 * and the value actually stored is the one the server returns.
 */
function slugify(value: string): string {
  return (
    value
      // FormD splits "à" into "a" + a combining accent, so the accent can be
      // dropped on its own and the base letter survives.
      .normalize("NFD")
      .replace(/[̀-ͯ]/g, "")
      .toLowerCase()
      .replace(/[^a-z0-9]+/g, "-")
      .replace(/^-+|-+$/g, "")
  );
}

/** Set as soon as the administrator edits the address by hand. */
const slugTouched = ref(false);

// The address follows the title until someone types their own, and never once
// the page has been published: after that the address belongs to whoever
// bookmarked it, and retyping the title must not silently move the page.
watch(title, (value) => {
  if (!slugTouched.value && !wasPublished.value) {
    slug.value = slugify(value);
  }
});

// Detecting the hand edit by comparison rather than by an event: the watcher
// above always leaves the field equal to the slugified title, so anything
// else can only have been typed.
watch(slug, (value) => {
  if (value !== slugify(title.value)) {
    slugTouched.value = true;
  }
});

/** True when the address of an already-published page is about to change. */
const slugChangeBreaksLinks = computed(
  () => wasPublished.value && originalSlug.value !== "" && slug.value !== originalSlug.value,
);

/** Empty string means "no value", which is not the same as zero. */
function toNumber(value: string): number | null {
  const trimmed = value.trim();
  return trimmed === "" ? null : Number(trimmed);
}

async function submit(): Promise<void> {
  fieldErrors.value = {};
  formError.value = undefined;

  const parsed = serviceFormSchema.safeParse({
    title: title.value,
    slug: slug.value,
    shortDescription: shortDescription.value,
    description: description.value,
    durationMinutes: toNumber(durationMinutes.value),
    price: toNumber(price.value),
    currency: price.value.trim() === "" ? null : currency.value,
    isPublished: isPublished.value,
    isBookable: isBookable.value,
    sortOrder: toNumber(sortOrder.value) ?? 0,
  });

  if (!parsed.success) {
    for (const issue of parsed.error.issues) {
      const field = issue.path[0];
      if (typeof field === "string") {
        fieldErrors.value[field] ??= issue.message;
      }
    }
    return;
  }

  try {
    const saved = await save.mutateAsync({
      ...(isNew.value ? {} : { id: serviceId.value }),
      model: {
        ...parsed.data,
        // An empty slug means "derive it from the title": the server owns
        // that rule, so the field is sent absent rather than blank.
        slug: parsed.data.slug === "" ? null : parsed.data.slug,
      },
    });

    // Creating lands on the edit page of what was just created, because the
    // next thing anyone does is add images — which need a service to exist.
    if (isNew.value) {
      await router.replace({ name: "services-admin-edit", params: { id: saved.id } });
      return;
    }

    originalSlug.value = saved.slug;
    wasPublished.value = saved.isPublished;
  } catch (error) {
    formError.value =
      error instanceof ValidationError
        ? "Controlla i dati inseriti e riprova."
        : ((error as Error).message ?? "Non siamo riusciti a salvare il servizio.");
  }
}
</script>

<template>
  <div class="space-y-6">
    <header>
      <h1 class="text-display font-semibold tracking-tight">
        {{ isNew ? "Nuovo servizio" : "Modifica servizio" }}
      </h1>
      <p class="mt-1 text-text-muted">Un servizio compare in vetrina solo quando è pubblicato.</p>
    </header>

    <p v-if="service.isPending.value && !isNew" class="text-sm text-text-muted">Caricamento…</p>

    <p v-else-if="service.isError.value" role="alert" class="text-sm text-danger">
      {{ service.error.value?.message }}
    </p>

    <form v-else class="space-y-6" @submit.prevent="submit">
      <Card title="Contenuto" heading-level="h2">
        <div class="space-y-4">
          <Input v-model="title" label="Titolo" required :error="fieldErrors['title']" />

          <Input
            v-model="slug"
            label="Indirizzo della pagina"
            hint="Lascialo vuoto e lo generiamo dal titolo."
            :error="fieldErrors['slug']"
          />

          <!-- A warning, not a rule: the address of a published page belongs
               to whoever linked it, but there are legitimate reasons to
               change it, and forbidding it would just move the problem. -->
          <p v-if="slugChangeBreaksLinks" role="status" class="text-sm text-warning">
            Stai cambiando l'indirizzo di una pagina già pubblicata: i link esistenti a
            <span class="font-medium">/{{ originalSlug }}</span> smetteranno di funzionare.
          </p>

          <Input
            v-model="shortDescription"
            label="Riga di presentazione"
            required
            hint="È quella che si legge nella scheda in elenco."
            :error="fieldErrors['shortDescription']"
          />

          <Textarea
            v-model="description"
            label="Descrizione"
            :maxlength="8000"
            :error="fieldErrors['description']"
          />
        </div>
      </Card>

      <Card title="Dati pratici" heading-level="h2">
        <div class="grid grid-cols-1 gap-4 sm:grid-cols-2">
          <Input
            v-model="durationMinutes"
            label="Durata (minuti)"
            type="number"
            hint="Serve per generare gli slot di prenotazione."
            :error="fieldErrors['durationMinutes']"
          />
          <Input
            v-model="sortOrder"
            label="Ordine in vetrina"
            type="number"
            hint="Più basso viene prima."
            :error="fieldErrors['sortOrder']"
          />
          <Input
            v-model="price"
            label="Prezzo"
            type="number"
            hint="Lascialo vuoto per non pubblicarlo."
            :error="fieldErrors['price']"
          />
          <Input
            v-model="currency"
            label="Valuta"
            hint="Codice ISO 4217, per esempio EUR."
            :error="fieldErrors['currency']"
          />
        </div>

        <div class="mt-4 space-y-1">
          <Checkbox
            v-model="isPublished"
            label="Pubblicato"
            hint="I visitatori lo vedranno in vetrina."
          />
          <Checkbox
            v-model="isBookable"
            label="Prenotabile"
            hint="Richiede una durata: è quella che genera gli slot."
            :error="fieldErrors['durationMinutes']"
          />
        </div>
      </Card>

      <ServiceGallery
        v-if="!isNew && service.data.value"
        :service-id="serviceId"
        :images="service.data.value.images"
      />

      <div class="flex flex-wrap items-center gap-3">
        <Button type="submit" :loading="save.isPending.value">Salva</Button>
        <Button variant="secondary" @click="router.back()">Annulla</Button>
        <p v-if="formError" role="alert" class="text-sm text-danger">{{ formError }}</p>
      </div>
    </form>
  </div>
</template>
