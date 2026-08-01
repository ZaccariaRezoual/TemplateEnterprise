<script setup lang="ts">
/**
 * -----------------------------------------------------------------------------
 * DashboardGrid
 * -----------------------------------------------------------------------------
 *
 * Renders the tiles the API returned, choosing a component per widget kind.
 *
 * Responsibilities:
 * - Loading, error and empty states of the dashboard.
 * - Dispatching each widget to the component that renders its kind.
 *
 * It holds no business logic and knows no module: which tiles exist is decided
 * server-side by whichever modules are installed, which is the whole point of
 * the widget contract. Adding a tile to the product means implementing
 * `IDashboardWidgetProvider` in a module — this file does not change.
 *
 * Layout (docs/design-system.md §12): one column on mobile, two from `sm`,
 * four from `lg`. List widgets are given two columns from `sm` upward, because
 * a column of truncated labels is unreadable at stat-tile width.
 */
import { Button, Card, Skeleton } from "@enterprise/ui";
import { useDashboardWidgets } from "../composables/useDashboard";
import ListWidget from "./ListWidget.vue";
import StatWidget from "./StatWidget.vue";

const { data, isPending, isError, refetch } = useDashboardWidgets();
</script>

<template>
  <div
    class="grid grid-cols-1 gap-4 sm:grid-cols-2 lg:grid-cols-4"
    :aria-busy="isPending"
    role="region"
    aria-label="Dashboard widgets"
  >
    <!-- Four placeholders: enough to fill the desktop row, and they collapse
         with the grid on narrower screens without any extra rule. -->
    <template v-if="isPending">
      <Card v-for="index in 4" :key="index">
        <Skeleton width="6rem" />
        <Skeleton shape="block" width="4rem" height="2rem" class="mt-3" />
        <Skeleton width="8rem" class="mt-3" />
      </Card>
    </template>

    <Card v-else-if="isError" class="sm:col-span-2 lg:col-span-4" title="Dashboard unavailable">
      <p class="text-sm text-text-muted">The widgets could not be loaded.</p>
      <template #footer>
        <Button variant="secondary" @click="() => refetch()">Retry</Button>
      </template>
    </Card>

    <Card v-else-if="!data || data.length === 0" class="sm:col-span-2 lg:col-span-4">
      <p class="text-sm text-text-muted">
        No widgets yet. Modules contribute them as they are installed and as your permissions allow.
      </p>
    </Card>

    <template v-else>
      <component
        :is="widget.kind === 'List' ? ListWidget : StatWidget"
        v-for="widget in data"
        :key="widget.id"
        :widget="widget"
        :class="widget.kind === 'List' ? 'sm:col-span-2' : undefined"
      />
    </template>
  </div>
</template>
