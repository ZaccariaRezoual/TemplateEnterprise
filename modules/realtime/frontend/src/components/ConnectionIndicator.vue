<script setup lang="ts">
/**
 * -----------------------------------------------------------------------------
 * ConnectionIndicator
 * -----------------------------------------------------------------------------
 *
 * Shows the realtime connection state.
 *
 * It renders NOTHING while connected: a permanent "everything is fine" badge
 * is noise, and users only need to know when live updates have stopped —
 * otherwise a stale table looks like an empty one.
 */
import { computed } from "vue";
import { useRealtime } from "../composables/useRealtime";

const { status } = useRealtime();

const message = computed(() => {
  switch (status.value) {
    case "reconnecting":
      return "Reconnecting…";
    case "disconnected":
      return "Live updates are off";
    default:
      return undefined;
  }
});
</script>

<template>
  <p
    v-if="message"
    role="status"
    class="flex items-center gap-1.5 rounded-md px-2 py-1 text-xs text-text-muted"
    data-testid="connection-indicator"
  >
    <span
      class="size-1.5 rounded-full"
      :class="status === 'reconnecting' ? 'bg-warning' : 'bg-danger'"
      aria-hidden="true"
    />
    {{ message }}
  </p>
</template>
