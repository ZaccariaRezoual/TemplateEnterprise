<script setup lang="ts">
/**
 * -----------------------------------------------------------------------------
 * Skeleton
 * -----------------------------------------------------------------------------
 *
 * Placeholder shown while content loads.
 *
 * Use it — rather than a spinner — whenever the eventual layout is known:
 * the space is reserved up front, so arriving data does not shift the page
 * (CLS), and the user sees the shape of what is coming instead of an
 * undifferentiated wait. A spinner remains the right choice only when the
 * result's shape is genuinely unknown.
 *
 * Accessibility: the element is `aria-hidden`, because a screen reader gains
 * nothing from "loading" repeated once per placeholder. The container that
 * swaps skeletons for content is responsible for announcing the state, e.g.
 * with `aria-busy` on the region.
 *
 * The pulse is suppressed under `prefers-reduced-motion`, per the motion
 * contract in docs/design-system.md.
 */
import { computed } from "vue";
import type { SkeletonProps } from "./Skeleton.types";

const props = withDefaults(defineProps<SkeletonProps>(), { shape: "text" });

const shapeClass = computed(() => {
  switch (props.shape) {
    case "circle":
      return "rounded-(--radius-pill) aspect-square";
    case "block":
      return "rounded-(--radius-control)";
    default:
      // A text placeholder mimics a line of body copy: the em-based height
      // keeps it aligned with the type scale instead of a fixed pixel value.
      return "rounded-(--radius-control) h-[1em]";
  }
});
</script>

<template>
  <div
    aria-hidden="true"
    class="animate-pulse bg-surface-sunken motion-reduce:animate-none"
    :class="shapeClass"
    :style="{ width, height }"
  />
</template>
