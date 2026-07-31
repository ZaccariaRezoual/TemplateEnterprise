<script setup lang="ts">
/**
 * -----------------------------------------------------------------------------
 * EchoForm
 * -----------------------------------------------------------------------------
 *
 * Form that sends a text to the echo endpoint and shows the result.
 *
 * Responsibilities:
 * - Local field state and client-side validation (fast feedback).
 * - Submitting through the `useEcho` mutation.
 * - Surfacing server-side field errors carried by `ValidationError`.
 * - Recording echoed texts in the feature's client-state store.
 *
 * It contains no business logic and no API knowledge: the request lives in the
 * composable, the route in the feature service.
 */
import { computed, ref } from "vue";
import { ValidationError } from "@/core/errors/applicationError";
import { useEcho } from "@/features/demo/composables/useDemo";
import { useDemoPreferencesStore } from "@/features/demo/stores/demoPreferences.store";
import { echoFormSchema } from "@/features/demo/validators/echo.validator";

const echo = useEcho();
const preferences = useDemoPreferencesStore();

const text = ref("");
const clientError = ref<string>();

/** Server-reported message for the `Text` field, when the API rejected it. */
const serverFieldError = computed(() => {
  const error = echo.error.value;
  return error instanceof ValidationError ? error.errors["Text"]?.[0] : undefined;
});

const fieldError = computed(() => clientError.value ?? serverFieldError.value);

/**
 * Validates the input and submits it.
 * Invalid input never reaches the network; server rejections are shown inline.
 */
async function submit(): Promise<void> {
  clientError.value = undefined;

  const parsed = echoFormSchema.safeParse({ text: text.value });
  if (!parsed.success) {
    clientError.value = parsed.error.issues[0]?.message;
    return;
  }

  const response = await echo.mutateAsync(parsed.data.text).catch(() => undefined);
  if (response !== undefined) {
    preferences.remember(response.text);
    text.value = "";
  }
}
</script>

<template>
  <form class="space-y-3" novalidate @submit.prevent="submit">
    <div>
      <label for="echo-text" class="block text-sm font-medium">Text to echo</label>
      <div class="mt-1.5 flex gap-2">
        <input
          id="echo-text"
          v-model="text"
          type="text"
          autocomplete="off"
          :aria-invalid="fieldError !== undefined"
          :aria-describedby="fieldError === undefined ? undefined : 'echo-text-error'"
          class="min-w-0 flex-1 rounded-md border border-border bg-surface px-3 py-2 text-sm"
          placeholder="Say something…"
        />
        <button
          type="submit"
          :disabled="echo.isPending.value"
          class="cursor-pointer rounded-md bg-primary px-4 py-2 text-sm font-medium text-on-primary transition-colors hover:bg-primary-hover disabled:cursor-not-allowed disabled:opacity-60"
        >
          {{ echo.isPending.value ? "Sending…" : "Send" }}
        </button>
      </div>

      <p v-if="fieldError" id="echo-text-error" class="mt-1.5 text-sm text-danger">
        {{ fieldError }}
      </p>
    </div>

    <p v-if="echo.data.value" aria-live="polite" class="text-sm text-text-muted">
      Server echoed
      <span class="font-medium text-text">“{{ echo.data.value.text }}”</span>
      ({{ echo.data.value.length }} characters).
    </p>
  </form>
</template>
