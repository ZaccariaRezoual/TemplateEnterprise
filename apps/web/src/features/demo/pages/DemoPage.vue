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
 * Every visual element comes from the design system; the page adds layout, not
 * styling primitives. Copy this structure when building a real feature.
 */
import { ArrowPathIcon } from "@heroicons/vue/24/outline";
import { Badge, Button, Card } from "@enterprise/ui";
import { formatDate } from "@enterprise/shared";
import { useSessionStore } from "@enterprise/module-auth";
import { ref } from "vue";
import EchoForm from "@/features/demo/components/EchoForm.vue";
import { demoApi } from "@/features/demo/api/demo.api";
import { usePing } from "@/features/demo/composables/useDemo";
import { useDemoPreferencesStore } from "@/features/demo/stores/demoPreferences.store";

const ping = usePing();
const preferences = useDemoPreferencesStore();
const session = useSessionStore();

const isNotifying = ref(false);

/**
 * Asks the API to notify the caller.
 *
 * Nothing here touches SignalR: the notification travels the event bus, gets
 * persisted, and comes back over the live connection to update the bell and
 * raise a toast — all handled by the modules that own those concerns.
 */
async function notifyMe(): Promise<void> {
  isNotifying.value = true;
  try {
    await demoApi.notifyMe("Realtime works", "This arrived over the live connection.");
  } finally {
    isNotifying.value = false;
  }
}
</script>

<template>
  <div class="space-y-8">
    <header>
      <h1 class="text-2xl font-semibold tracking-tight">Demo feature</h1>
      <p class="mt-1 text-text-muted">
        Vertical slice through the stack: page → composable → feature service → SDK → API.
      </p>
    </header>

    <Card title="Server state (TanStack Query)" heading-level="h2">
      <template #actions>
        <Button
          variant="secondary"
          size="sm"
          :disabled="ping.isFetching.value"
          @click="ping.refetch()"
        >
          <template #icon>
            <ArrowPathIcon class="size-4" :class="{ 'animate-spin': ping.isFetching.value }" />
          </template>
          Refetch
        </Button>
      </template>

      <p v-if="ping.isPending.value" class="text-sm text-text-muted">Loading…</p>

      <p v-else-if="ping.isError.value" class="text-sm text-danger">
        {{ ping.error.value?.message }}
      </p>

      <dl v-else-if="ping.data.value" class="grid gap-3 text-sm sm:grid-cols-2">
        <div>
          <dt class="text-text-muted">Message</dt>
          <dd class="mt-0.5">
            <Badge variant="success">{{ ping.data.value.message }}</Badge>
          </dd>
        </div>
        <div>
          <dt class="text-text-muted">Server time</dt>
          <dd class="mt-0.5 font-medium tabular-nums">
            {{
              formatDate(ping.data.value.timestampUtc, { dateStyle: "medium", timeStyle: "medium" })
            }}
          </dd>
        </div>
      </dl>
    </Card>

    <Card
      title="Mutation with validation"
      description="Submitting publishes a domain event on the server and invalidates the query above."
      heading-level="h2"
    >
      <EchoForm />
    </Card>

    <Card
      v-if="session.isAuthenticated"
      title="Realtime"
      description="The API publishes a notification on the event bus; it comes back over the live connection and updates the bell without a refresh."
      heading-level="h2"
    >
      <Button :disabled="isNotifying" data-testid="notify-me" @click="notifyMe">
        {{ isNotifying ? "Sending…" : "Notify me" }}
      </Button>
    </Card>

    <Card title="Client state (Pinia)" heading-level="h2">
      <template #actions>
        <Button
          variant="secondary"
          size="sm"
          :aria-expanded="preferences.isHistoryVisible"
          aria-controls="echo-history"
          @click="preferences.toggleHistory()"
        >
          {{ preferences.isHistoryVisible ? "Hide" : "Show" }}
        </Button>
      </template>

      <div v-show="preferences.isHistoryVisible" id="echo-history">
        <ul v-if="preferences.history.length > 0" class="space-y-1 text-sm">
          <li v-for="(entry, index) in preferences.history" :key="`${index}-${entry}`">
            {{ entry }}
          </li>
        </ul>
        <p v-else class="text-sm text-text-muted">Nothing echoed yet in this session.</p>
      </div>
    </Card>
  </div>
</template>
