<script setup lang="ts">
/**
 * -----------------------------------------------------------------------------
 * PageSection
 * -----------------------------------------------------------------------------
 *
 * A titled band of a page: heading, intro, and whatever the caller puts
 * inside.
 *
 * It exists so vertical rhythm is decided once. Every page repeating its own
 * padding is how a product ends up with six slightly different spacings, and
 * with a rebrand that has to touch every page. It is the only component
 * allowed to decide the vertical spacing of a band.
 *
 * It reads `py-band` / `py-band-lg`, which the PUBLIC surface redefines: the
 * same markup is a generous marketing band outside `/admin` and a compact one
 * inside it. A page does not know its own surface, and does not need to.
 *
 * Started life in the Site module and moved here the day a second surface —
 * the Services showcase — needed it, which is the rule the design system
 * states for promoting a component.
 */
import { cn } from "../../utils/cn";
import type { PageSectionProps } from "./PageSection.types";

withDefaults(defineProps<PageSectionProps>(), {
  // `title` and `intro` default to undefined on purpose: a band of pure
  // content has neither, and an empty string would still render the element.
  title: undefined,
  intro: undefined,
  headingLevel: "h2",
});
</script>

<template>
  <section class="mx-auto max-w-5xl px-4 py-band sm:px-6 sm:py-band-lg">
    <component
      :is="headingLevel"
      v-if="title"
      :class="
        cn('font-semibold tracking-tight', headingLevel === 'h1' ? 'text-display' : 'text-heading')
      "
    >
      {{ title }}
    </component>

    <!-- `max-w-prose` keeps a line between 45 and 75 characters, which is
         where prose stays readable — a full-width paragraph on a laptop is
         the most common readability mistake on a marketing page. -->
    <p v-if="intro" class="mt-3 max-w-prose text-text-muted">{{ intro }}</p>

    <div v-if="$slots.default" :class="title || intro ? 'mt-8' : ''">
      <slot />
    </div>
  </section>
</template>
