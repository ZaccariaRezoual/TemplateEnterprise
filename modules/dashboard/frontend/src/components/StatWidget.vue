<script setup lang="ts">
/**
 * -----------------------------------------------------------------------------
 * StatWidget
 * -----------------------------------------------------------------------------
 *
 * A single figure with an optional supporting line — the dashboard's default
 * tile.
 *
 * Design notes (docs/design-system.md):
 * - The value uses `tabular-nums`, so a counter changing from 9 to 10 does not
 *   shift the layout under the user's eye.
 * - The whole tile is the link when one is provided, giving a target far
 *   larger than the 44px minimum instead of a small text link.
 * - No shadow: cards are flat by default, and a grid of shadowed tiles is a
 *   grid where nothing is emphasised.
 */
import { Card } from "@enterprise/ui";
import { computed } from "vue";
import { resolveWidgetLink, type DashboardWidget } from "../api/dashboard.api";

const props = defineProps<{ widget: DashboardWidget }>();

// The server names a route; the host decides where that route lives.
const target = computed(() => resolveWidgetLink(props.widget.link));
</script>

<template>
  <component :is="target ? 'RouterLink' : 'div'" :to="target" class="block rounded-(--card-radius)">
    <Card>
      <p class="text-sm text-text-muted">{{ widget.title }}</p>
      <p class="mt-1 text-3xl font-semibold tracking-tight tabular-nums">
        {{ widget.value ?? "—" }}
      </p>
      <p v-if="widget.caption" class="mt-1 text-sm text-text-muted">{{ widget.caption }}</p>
    </Card>
  </component>
</template>
