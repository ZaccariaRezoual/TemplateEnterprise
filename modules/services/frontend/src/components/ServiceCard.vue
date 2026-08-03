<script setup lang="ts">
/**
 * -----------------------------------------------------------------------------
 * ServiceCard
 * -----------------------------------------------------------------------------
 *
 * One service in the showcase list: cover image, title, one line, and the
 * practical facts when they are published.
 *
 * The whole card is one link. A card with a separate "read more" link gives
 * the visitor a small target next to a large dead area, which on a phone is
 * the difference between opening the page and tapping nothing.
 *
 * Duration and price render only when present: a project that does not
 * publish prices must not see an empty row where they would be.
 */
import { computed } from "vue";
import { RouterLink } from "vue-router";
import { serviceImageUrl, type PublicService } from "../api/services.api";

const props = defineProps<{
  /** The service to show. */
  service: PublicService;
}>();

/** The cover, which the API already sorted to the front of the gallery. */
const cover = computed(() => props.service.images[0]);

/** Duration and price, joined only when both are published. */
const facts = computed(() => {
  const parts: string[] = [];

  if (props.service.durationMinutes !== null && props.service.durationMinutes !== undefined) {
    parts.push(`${props.service.durationMinutes} min`);
  }
  if (props.service.price !== null && props.service.price !== undefined) {
    parts.push(`${props.service.price} ${props.service.currency ?? ""}`.trim());
  }

  return parts.join(" · ");
});
</script>

<template>
  <RouterLink
    :to="{ name: 'services-detail', params: { slug: service.slug } }"
    class="group block overflow-hidden rounded-(--card-radius) border border-border bg-surface transition-colors hover:border-primary"
  >
    <img
      v-if="cover"
      :src="serviceImageUrl(cover.storageFileId)"
      :alt="cover.altText"
      class="aspect-video w-full bg-surface-sunken object-cover"
      loading="lazy"
    />

    <div class="p-5">
      <h3 class="text-title font-semibold tracking-tight">{{ service.title }}</h3>
      <p class="mt-2 text-text-muted">{{ service.shortDescription }}</p>
      <p v-if="facts" class="mt-3 text-sm text-text-muted tabular-nums">{{ facts }}</p>
    </div>
  </RouterLink>
</template>
