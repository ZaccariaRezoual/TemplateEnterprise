<script setup lang="ts">
/**
 * -----------------------------------------------------------------------------
 * Button
 * -----------------------------------------------------------------------------
 *
 * The action primitive of the design system.
 *
 * Responsibilities:
 * - Renders a native `<button>` with the variant/size styling.
 * - Blocks interaction while disabled or loading, and reports both states to
 *   assistive technology (`disabled`, `aria-busy`).
 *
 * Use it for actions. When the action IS a navigation, pass `href`: the
 * component then renders an `<a>` with the same styling, because a
 * `<button>` that navigates breaks middle-click, "open in new tab" and
 * copy-link — and hand-styling a link would put literal values in a page.
 *
 * All styling resolves through semantic tokens, so it inherits any theme
 * without modification.
 */
import { computed, useAttrs } from "vue";
import { cn } from "../../utils/cn";
import type { ButtonEmits, ButtonProps } from "./Button.types";

// Attribute inheritance is disabled so the consumer's `class` goes THROUGH
// twMerge instead of being appended after it. Otherwise both `bg-primary` and
// a consumer's `bg-surface` are emitted and the winner depends on CSS source
// order — a conflict that is invisible until it renders wrong.
defineOptions({ inheritAttrs: false });

const props = withDefaults(defineProps<ButtonProps>(), {
  variant: "primary",
  size: "md",
  type: "button",
  disabled: false,
  loading: false,
  block: false,
});

const emit = defineEmits<ButtonEmits>();
const attrs = useAttrs();

const VARIANT_CLASSES: Record<NonNullable<ButtonProps["variant"]>, string> = {
  primary: "bg-primary text-on-primary hover:bg-primary-hover",
  secondary: "border border-border bg-surface text-text hover:bg-background",
  ghost: "text-text-muted hover:bg-background hover:text-text",
  danger: "bg-danger text-on-status hover:opacity-90",
};

const SIZE_CLASSES: Record<NonNullable<ButtonProps["size"]>, string> = {
  sm: "h-8 gap-1.5 px-3 text-sm",
  md: "h-10 gap-2 px-4 text-sm",
  lg: "h-12 gap-2 px-6 text-base",
};

/** Interaction is blocked while an operation is in flight, not just when disabled. */
const isInteractionBlocked = computed(() => props.disabled || props.loading);

const classes = computed(() =>
  cn(
    "inline-flex cursor-pointer items-center justify-center rounded-(--button-radius) font-medium transition-colors",
    "disabled:cursor-not-allowed disabled:opacity-60",
    VARIANT_CLASSES[props.variant],
    SIZE_CLASSES[props.size],
    props.block && "w-full",
    attrs["class"] as string | undefined,
  ),
);

/** Every attribute except `class`, which is merged above. */
const forwardedAttrs = computed(() => {
  const { class: _class, ...rest } = attrs;
  return rest;
});

/** True when this instance is a navigation, and therefore an anchor. */
const isLink = computed(() => props.href !== undefined);

function onClick(event: MouseEvent): void {
  if (isInteractionBlocked.value) {
    // An anchor has no `disabled` attribute, so the navigation has to be
    // stopped here or a "disabled" link still navigates.
    event.preventDefault();
    return;
  }
  emit("click", event);
}
</script>

<template>
  <component
    :is="isLink ? 'a' : 'button'"
    v-bind="forwardedAttrs"
    :href="isLink ? href : undefined"
    :type="isLink ? undefined : type"
    :class="classes"
    :disabled="isLink ? undefined : isInteractionBlocked"
    :aria-disabled="isLink && isInteractionBlocked ? 'true' : undefined"
    :aria-busy="loading ? 'true' : undefined"
    @click="onClick"
  >
    <svg
      v-if="loading"
      class="size-4 animate-spin"
      viewBox="0 0 24 24"
      fill="none"
      aria-hidden="true"
    >
      <circle class="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" stroke-width="4" />
      <path class="opacity-75" fill="currentColor" d="M4 12a8 8 0 0 1 8-8v4a4 4 0 0 0-4 4H4z" />
    </svg>
    <slot v-else name="icon" />
    <slot />
  </component>
</template>
