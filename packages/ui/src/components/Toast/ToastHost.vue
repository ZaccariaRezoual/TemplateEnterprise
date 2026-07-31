<script setup lang="ts">
/**
 * -----------------------------------------------------------------------------
 * ToastHost
 * -----------------------------------------------------------------------------
 *
 * Renders the toast stack. Mount it ONCE, near the root of the application.
 *
 * Responsibilities:
 * - Displays the toasts raised through `useToast`.
 * - Provides the dismiss affordance.
 *
 * Accessibility decisions that matter here: the region is a `status` live
 * region so new toasts are announced without stealing focus, errors use
 * `alert` so they interrupt, and every toast has a real dismiss button —
 * relying on the auto-timeout alone excludes anyone who reads slowly.
 */
import { computed } from "vue";
import { cn } from "../../utils/cn";
import type { IToastHostProps, ToastVariant } from "./Toast.types";
import { useToast } from "./useToast";

const props = withDefaults(defineProps<IToastHostProps>(), { max: 4 });

const { toasts, dismiss } = useToast();

/** Newest last, capped: an unbounded stack covers the UI it describes. */
const visible = computed(() => toasts.value.slice(-props.max));

const VARIANT_CLASSES: Record<ToastVariant, string> = {
  info: "border-border bg-surface-raised text-text",
  success: "border-success/40 bg-surface-raised text-text",
  warning: "border-warning/40 bg-surface-raised text-text",
  danger: "border-danger/40 bg-surface-raised text-text",
};

const ACCENT_CLASSES: Record<ToastVariant, string> = {
  info: "bg-primary",
  success: "bg-success",
  warning: "bg-warning",
  danger: "bg-danger",
};
</script>

<template>
  <div
    class="pointer-events-none fixed inset-x-0 bottom-0 z-50 flex flex-col items-center gap-2 p-4 sm:items-end"
    role="status"
    aria-live="polite"
    aria-label="Notifications"
    data-testid="toast-host"
  >
    <div
      v-for="toast in visible"
      :key="toast.id"
      :role="toast.variant === 'danger' ? 'alert' : undefined"
      class="pointer-events-auto flex w-full max-w-sm gap-3 overflow-hidden rounded-lg border shadow-lg"
      :class="cn(VARIANT_CLASSES[toast.variant])"
      data-testid="toast"
    >
      <span class="w-1 shrink-0" :class="ACCENT_CLASSES[toast.variant]" aria-hidden="true" />

      <div class="min-w-0 flex-1 py-3">
        <p class="text-sm font-medium">{{ toast.title }}</p>
        <p v-if="toast.description" class="mt-0.5 text-sm text-text-muted">
          {{ toast.description }}
        </p>
      </div>

      <button
        type="button"
        class="shrink-0 cursor-pointer px-3 text-text-muted transition-colors hover:text-text"
        :aria-label="`Dismiss: ${toast.title}`"
        @click="dismiss(toast.id)"
      >
        <span aria-hidden="true">×</span>
      </button>
    </div>
  </div>
</template>
