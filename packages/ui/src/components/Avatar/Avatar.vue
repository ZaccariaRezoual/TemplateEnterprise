<script setup lang="ts">
/**
 * -----------------------------------------------------------------------------
 * Avatar
 * -----------------------------------------------------------------------------
 *
 * Represents a person or entity with a picture, falling back to initials.
 *
 * Responsibilities:
 * - Renders the image and swaps to initials when it is missing OR fails to
 *   load at runtime (broken URLs are the common case, not the exception).
 * - Provides an accessible name, or hides itself when the name is already
 *   shown next to it.
 */
import { computed, ref, useAttrs, watch } from "vue";
import { cn } from "../../utils/cn";
import type { AvatarProps } from "./Avatar.types";

// See Button.vue: the consumer's `class` must go through twMerge, not be
// appended after it, or conflicting utilities resolve by CSS source order.
defineOptions({ inheritAttrs: false });

const props = withDefaults(defineProps<AvatarProps>(), {
  size: "md",
  decorative: false,
});

const attrs = useAttrs();

const hasImageFailed = ref(false);

// A new src deserves a fresh attempt; otherwise one broken URL would
// permanently pin the component to its fallback.
watch(
  () => props.src,
  () => {
    hasImageFailed.value = false;
  },
);

const showImage = computed(
  () => props.src !== undefined && props.src !== "" && !hasImageFailed.value,
);

/** First letters of the first and last words, e.g. "Ada Lovelace" → "AL". */
const initials = computed(() => {
  const words = props.name.trim().split(/\s+/).filter(Boolean);
  const first = words[0]?.[0] ?? "";
  const last = words.length > 1 ? (words[words.length - 1]?.[0] ?? "") : "";
  return (first + last).toUpperCase();
});

const SIZE_CLASSES: Record<NonNullable<AvatarProps["size"]>, string> = {
  sm: "size-8 text-xs",
  md: "size-10 text-sm",
  lg: "size-14 text-base",
};

const classes = computed(() =>
  cn(
    "inline-flex shrink-0 items-center justify-center overflow-hidden rounded-full",
    "bg-background font-medium text-text-muted select-none",
    SIZE_CLASSES[props.size],
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
  <span
    v-bind="forwardedAttrs"
    :class="classes"
    :role="decorative ? 'presentation' : 'img'"
    :aria-label="decorative ? undefined : name"
    :aria-hidden="decorative ? 'true' : undefined"
  >
    <img
      v-if="showImage"
      :src="src"
      alt=""
      class="size-full object-cover"
      @error="hasImageFailed = true"
    />
    <span v-else aria-hidden="true">{{ initials }}</span>
  </span>
</template>
