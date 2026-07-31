<script setup lang="ts">
/**
 * -----------------------------------------------------------------------------
 * Badge
 * -----------------------------------------------------------------------------
 *
 * Compact status indicator.
 *
 * Responsibilities:
 * - Renders a short label with the styling of a semantic variant.
 * - Optionally exposes a screen-reader-only label so status is never conveyed
 *   by color alone (WCAG 1.4.1).
 *
 * It is presentational: it never derives its own state.
 */
import { computed, useAttrs } from "vue";
import { cn } from "../../utils/cn";
import type { BadgeProps } from "./Badge.types";

// See Button.vue: the consumer's `class` must go through twMerge, not be
// appended after it, or conflicting utilities resolve by CSS source order.
defineOptions({ inheritAttrs: false });

const props = withDefaults(defineProps<BadgeProps>(), { variant: "neutral" });
const attrs = useAttrs();

// Status colors are tinted with a transparency of the semantic token rather
// than a separate palette, so a rebrand propagates here automatically.
const VARIANT_CLASSES: Record<NonNullable<BadgeProps["variant"]>, string> = {
  neutral: "bg-background text-text-muted border-border",
  success: "bg-success/10 text-success border-success/30",
  warning: "bg-warning/10 text-warning border-warning/30",
  danger: "bg-danger/10 text-danger border-danger/30",
  info: "bg-primary/10 text-primary border-primary/30",
};

const classes = computed(() =>
  cn(
    "inline-flex items-center rounded-full border px-2 py-0.5 text-xs font-medium",
    VARIANT_CLASSES[props.variant],
    attrs["class"] as string | undefined,
  ),
);

/** Every attribute except `class`, which is merged above. */
const forwardedAttrs = computed(() => {
  const { class: _class, ...rest } = attrs;
  return rest;
});
</script>

<template>
  <span v-bind="forwardedAttrs" :class="classes">
    <span v-if="srLabel" class="sr-only">{{ srLabel }}</span>
    <slot />
  </span>
</template>
