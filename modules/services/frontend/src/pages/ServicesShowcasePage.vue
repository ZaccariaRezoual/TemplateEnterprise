<script setup lang="ts">
/**
 * -----------------------------------------------------------------------------
 * ServicesShowcasePage
 * -----------------------------------------------------------------------------
 *
 * The public list of services: what the visitor sees at `/services`.
 *
 * Responsibilities:
 * - Reads the published catalogue through the module's composable.
 * - Renders it as a grid of cards, and says something useful when there is
 *   nothing to show.
 *
 * The endpoint it reads cannot return a draft — that guarantee lives in the
 * backend query, not in this page — so there is no filtering here at all.
 *
 * One column on a phone, two from `sm`, three from `lg` and no further: past
 * three columns the line of prose inside a card stops being readable, which
 * is the same rule the public design system states for `HighlightGrid`.
 */
import { PageSection, Skeleton } from "@enterprise/ui";
import ServiceCard from "../components/ServiceCard.vue";
import { usePublicServices } from "../composables/useServices";

const services = usePublicServices();
</script>

<template>
  <PageSection
    title="Servizi"
    intro="Quello che facciamo, con durata e prezzo dove sono pubblicati."
    heading-level="h1"
  >
    <!-- Skeletons rather than a spinner: the shape of the result is known, so
         the space is reserved in advance and the cards do not shove the page
         down when they arrive. -->
    <div
      v-if="services.isPending.value"
      class="grid grid-cols-1 gap-6 sm:grid-cols-2 lg:grid-cols-3"
    >
      <Skeleton v-for="placeholder in 3" :key="placeholder" class="h-64 w-full" />
    </div>

    <p v-else-if="services.isError.value" role="alert" class="text-danger">
      Non siamo riusciti a caricare i servizi. Riprova fra poco.
    </p>

    <p v-else-if="services.data.value?.length === 0" class="max-w-prose text-text-muted">
      Non ci sono ancora servizi pubblicati.
    </p>

    <ul v-else class="grid grid-cols-1 gap-6 sm:grid-cols-2 lg:grid-cols-3">
      <li v-for="service in services.data.value" :key="service.id">
        <ServiceCard :service="service" />
      </li>
    </ul>
  </PageSection>
</template>
