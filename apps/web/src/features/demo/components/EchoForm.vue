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
 * composable, the operation in the feature service. Field markup, labelling
 * and error wiring come from the design system.
 */
import { Button, Input } from "@enterprise/ui";
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
    <div class="flex items-end gap-2">
      <Input
        v-model="text"
        label="Text to echo"
        placeholder="Say something…"
        autocomplete="off"
        :error="fieldError"
      />
      <Button type="submit" :loading="echo.isPending.value" class="shrink-0">
        {{ echo.isPending.value ? "Sending…" : "Send" }}
      </Button>
    </div>

    <p v-if="echo.data.value" aria-live="polite" class="text-sm text-text-muted">
      Server echoed
      <span class="font-medium text-text">“{{ echo.data.value.text }}”</span>
      ({{ echo.data.value.length }} characters).
    </p>
  </form>
</template>
