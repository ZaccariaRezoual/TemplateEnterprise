<script setup lang="ts">
/**
 * -----------------------------------------------------------------------------
 * ListWidget
 * -----------------------------------------------------------------------------
 *
 * A short list of recent items.
 *
 * Design notes (docs/design-system.md):
 * - Long labels truncate rather than wrap, so one unusually long entry cannot
 *   change the height of the tile and reflow the grid around it.
 * - An empty list says so explicitly. A blank tile reads as broken.
 */
import { Card } from "@enterprise/ui";
import type { DashboardWidget } from "../api/dashboard.api";

defineProps<{ widget: DashboardWidget }>();
</script>

<template>
  <Card :title="widget.title" heading-level="h3">
    <ul v-if="widget.items && widget.items.length > 0" class="space-y-2">
      <li
        v-for="(item, index) in widget.items"
        :key="`${index}-${item.label}`"
        class="flex items-baseline justify-between gap-3 text-sm"
      >
        <span class="truncate">{{ item.label }}</span>
        <span v-if="item.detail" class="shrink-0 text-xs text-text-muted tabular-nums">
          {{ item.detail }}
        </span>
      </li>
    </ul>
    <p v-else class="text-sm text-text-muted">Nothing recorded yet.</p>
  </Card>
</template>
