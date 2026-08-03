<script setup lang="ts">
/**
 * -----------------------------------------------------------------------------
 * ServiceDetailPage
 * -----------------------------------------------------------------------------
 *
 * The public page of one service: gallery, description, practical facts and a
 * single call to action.
 *
 * Responsibilities:
 * - Reads the service behind the address in the route.
 * - Sets the document title and description from the DATA once it arrives —
 *   the route can only declare fixed strings, and without this every service
 *   shared on Slack would preview with the same generic title.
 * - Says plainly when the address does not exist, instead of showing an empty
 *   page.
 *
 * **One** call to action, per the public design system: a page that asks the
 * visitor to choose between three equivalent buttons gets no decision at all.
 * Where it leads is decided in `ctaTarget` — the booking flow for a bookable
 * service, the contact page for one that is only described.
 */
import { Button, PageSection, Skeleton } from "@enterprise/ui";
import { NotFoundError } from "@enterprise/shared";
import { computed, watch } from "vue";
import { RouterLink, useRoute } from "vue-router";
import { serviceImageUrl } from "../api/services.api";
import { usePublicService } from "../composables/useServices";
import { applyRuntimeSeo } from "../install";

const route = useRoute();

const slug = computed(() => (typeof route.params["slug"] === "string" ? route.params["slug"] : ""));
const service = usePublicService(slug);

/** True when the address simply does not exist, as opposed to a real failure. */
const isMissing = computed(() => service.error.value instanceof NotFoundError);

/** Duration and price, each shown only when it was published. */
const facts = computed(() => {
  const loaded = service.data.value;
  if (loaded === undefined) {
    return [];
  }

  const rows: { label: string; value: string }[] = [];

  if (loaded.durationMinutes !== null && loaded.durationMinutes !== undefined) {
    rows.push({ label: "Durata", value: `${loaded.durationMinutes} minuti` });
  }
  if (loaded.price !== null && loaded.price !== undefined) {
    rows.push({ label: "Prezzo", value: `${loaded.price} ${loaded.currency ?? ""}`.trim() });
  }

  return rows;
});

/**
 * Where the single call to action leads.
 *
 * A bookable service goes to the booking flow; anything else goes to the
 * contact page, because there is nothing to reserve. The service id travels
 * in the query string: the URL is addressed by slug, which reads well and
 * survives being shared, while the availability endpoint works by id.
 */
const ctaTarget = computed(() => {
  const loaded = service.data.value;

  return loaded?.isBookable === true ? `/book/${loaded.slug}?serviceId=${loaded.id}` : "/contact";
});

// Metadata follows the data, not the route: it can only be set once the
// service has been loaded, and it has to be reset when the visitor navigates
// from one service to another without leaving the page.
watch(
  () => service.data.value,
  (loaded) => {
    if (loaded !== undefined) {
      applyRuntimeSeo({ title: loaded.title, description: loaded.shortDescription });
    }
  },
  { immediate: true },
);
</script>

<template>
  <PageSection :title="service.data.value?.title" heading-level="h1">
    <template v-if="service.isPending.value">
      <Skeleton class="h-72 w-full" />
      <Skeleton class="mt-6 h-24 w-full max-w-prose" />
    </template>

    <template v-else-if="isMissing">
      <p class="max-w-prose text-text-muted">
        Questo servizio non esiste, o non è più disponibile.
      </p>
      <RouterLink v-slot="{ href, navigate }" :to="{ name: 'services-showcase' }" custom>
        <Button :href="href" variant="secondary" class="mt-6" @click="navigate">
          Torna ai servizi
        </Button>
      </RouterLink>
    </template>

    <p v-else-if="service.isError.value" role="alert" class="text-danger">
      Non siamo riusciti a caricare questo servizio. Riprova fra poco.
    </p>

    <template v-else-if="service.data.value">
      <!-- The cover comes first: the API sorts it to the front so the page
           does not have to know which one it is. -->
      <div
        v-if="service.data.value.images.length > 0"
        class="grid grid-cols-1 gap-4 sm:grid-cols-2"
      >
        <img
          v-for="image in service.data.value.images"
          :key="image.id"
          :src="serviceImageUrl(image.storageFileId)"
          :alt="image.altText"
          class="aspect-video w-full rounded-(--card-radius) bg-surface-sunken object-cover"
          loading="lazy"
        />
      </div>

      <!-- The lead reads as the lead by being the only full-contrast
           paragraph, not by being a size the type roles do not define. -->
      <p class="mt-8 max-w-prose font-medium text-text">
        {{ service.data.value.shortDescription }}
      </p>

      <!-- `whitespace-pre-line` because the description is plain text: the
           paragraph breaks the author typed are the only structure it has. -->
      <p
        v-if="service.data.value.description"
        class="mt-4 max-w-prose whitespace-pre-line text-text-muted"
      >
        {{ service.data.value.description }}
      </p>

      <dl v-if="facts.length > 0" class="mt-8 grid max-w-prose grid-cols-2 gap-4">
        <div v-for="fact in facts" :key="fact.label">
          <dt class="text-sm text-text-muted">{{ fact.label }}</dt>
          <dd class="mt-1 font-medium tabular-nums">{{ fact.value }}</dd>
        </div>
      </dl>

      <RouterLink v-slot="{ href, navigate }" :to="ctaTarget" custom>
        <Button :href="href" class="mt-10" @click="navigate">
          {{ service.data.value.isBookable ? "Prenota questo servizio" : "Parliamone" }}
        </Button>
      </RouterLink>
    </template>
  </PageSection>
</template>
