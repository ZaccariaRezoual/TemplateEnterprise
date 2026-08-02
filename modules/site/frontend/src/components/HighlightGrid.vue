<script setup lang="ts">
/**
 * -----------------------------------------------------------------------------
 * HighlightGrid
 * -----------------------------------------------------------------------------
 *
 * The list of selling points, services or values of a page, as cards.
 *
 * One column on a phone, two from `sm`, three from `lg` — three is the widest
 * a line of prose stays readable at this measure, which is why the grid stops
 * there instead of following the container.
 *
 * Renders nothing when the list is empty: a page whose content is just an
 * intro should show the intro, not an empty rail of boxes.
 */
import { Card } from "@enterprise/ui";
import type { SiteHighlight } from "../content";

defineProps<{
  /** Entries to render. */
  items: readonly SiteHighlight[];
}>();
</script>

<template>
  <ul v-if="items.length > 0" class="grid grid-cols-1 gap-4 sm:grid-cols-2 lg:grid-cols-3">
    <li v-for="item in items" :key="item.title">
      <Card :title="item.title" heading-level="h3">
        <p class="text-sm text-text-muted">{{ item.body }}</p>
      </Card>
    </li>
  </ul>
</template>
