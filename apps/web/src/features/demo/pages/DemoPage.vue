<script setup lang="ts">
/**
 * -----------------------------------------------------------------------------
 * DemoPage
 * -----------------------------------------------------------------------------
 *
 * Reference page of the framework: proves the full frontend vertical slice
 * against the running API.
 *
 * Responsibilities:
 * - Reads server state through the `usePing` query (loading/error/success).
 * - Hosts the echo form.
 * - Renders the client-only echo history from the feature's Pinia store.
 *
 * Copy this structure when building a real feature; it holds no business logic
 * itself. Removed once real features exist.
 */
import { ArrowPathIcon } from "@heroicons/vue/24/outline";
import { usePing } from "@/features/demo/composables/useDemo";
import EchoForm from "@/features/demo/components/EchoForm.vue";
import { useDemoPreferencesStore } from "@/features/demo/stores/demoPreferences.store";

const ping = usePing();
const preferences = useDemoPreferencesStore();
</script>

<template>
  <div class="space-y-8">
    <header>
      <h1 class="text-2xl font-semibold tracking-tight">Demo feature</h1>
      <p class="mt-1 text-text-muted">
        Vertical slice through the stack: page → composable → feature service → HTTP client → API.
      </p>
    </header>

    <section class="rounded-xl border border-border bg-surface p-5" aria-labelledby="ping-heading">
      <div class="flex items-center justify-between gap-4">
        <h2 id="ping-heading" class="font-medium">Server state (TanStack Query)</h2>
        <button
          type="button"
          class="inline-flex cursor-pointer items-center gap-1.5 rounded-md border border-border px-2.5 py-1.5 text-sm text-text-muted transition-colors hover:text-text"
          :disabled="ping.isFetching.value"
          @click="ping.refetch()"
        >
          <ArrowPathIcon class="size-4" :class="{ 'animate-spin': ping.isFetching.value }" />
          Refetch
        </button>
      </div>

      <p v-if="ping.isPending.value" class="mt-3 text-sm text-text-muted">Loading…</p>

      <p v-else-if="ping.isError.value" class="mt-3 text-sm text-danger">
        {{ ping.error.value?.message }}
      </p>

      <dl v-else-if="ping.data.value" class="mt-3 grid gap-2 text-sm sm:grid-cols-2">
        <div>
          <dt class="text-text-muted">Message</dt>
          <dd class="font-medium">{{ ping.data.value.message }}</dd>
        </div>
        <div>
          <dt class="text-text-muted">Server time (UTC)</dt>
          <dd class="font-medium tabular-nums">{{ ping.data.value.timestampUtc }}</dd>
        </div>
      </dl>
    </section>

    <section class="rounded-xl border border-border bg-surface p-5" aria-labelledby="echo-heading">
      <h2 id="echo-heading" class="font-medium">Mutation with validation</h2>
      <p class="mt-1 mb-4 text-sm text-text-muted">
        Submitting publishes a domain event on the server and invalidates the query above.
      </p>
      <EchoForm />
    </section>

    <section
      class="rounded-xl border border-border bg-surface p-5"
      aria-labelledby="history-heading"
    >
      <div class="flex items-center justify-between gap-4">
        <h2 id="history-heading" class="font-medium">Client state (Pinia)</h2>
        <button
          type="button"
          class="cursor-pointer rounded-md border border-border px-2.5 py-1.5 text-sm text-text-muted transition-colors hover:text-text"
          :aria-expanded="preferences.isHistoryVisible"
          aria-controls="echo-history"
          @click="preferences.toggleHistory()"
        >
          {{ preferences.isHistoryVisible ? "Hide" : "Show" }}
        </button>
      </div>

      <div v-show="preferences.isHistoryVisible" id="echo-history" class="mt-3">
        <ul v-if="preferences.history.length > 0" class="space-y-1 text-sm">
          <li v-for="(entry, index) in preferences.history" :key="`${index}-${entry}`">
            {{ entry }}
          </li>
        </ul>
        <p v-else class="text-sm text-text-muted">Nothing echoed yet in this session.</p>
      </div>
    </section>
  </div>
</template>
