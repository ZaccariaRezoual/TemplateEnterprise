<script setup lang="ts">
/**
 * -----------------------------------------------------------------------------
 * PageSection
 * -----------------------------------------------------------------------------
 *
 * A titled band of the public site: heading, intro, and whatever the caller
 * puts inside.
 *
 * It exists so vertical rhythm is decided once. Every page repeating its own
 * padding is how a site ends up with six slightly different spacings, and
 * with a rebrand that has to touch every page.
 *
 * The heading level is a prop because the outline of a page is not a visual
 * choice: a section under an `h1` needs an `h2`, and a component that always
 * emits `h2` quietly breaks the document outline.
 */
withDefaults(
  defineProps<{
    /** Section heading. Omit for a band of pure content. */
    title?: string | undefined;
    /** Sentence under the heading. */
    intro?: string | undefined;
    /** Heading level, so the page outline stays correct. Defaults to `h2`. */
    headingLevel?: "h1" | "h2" | "h3" | undefined;
  }>(),
  // `title` and `intro` default to undefined on purpose: a band of pure
  // content has neither, and an empty string would still render the element.
  { title: undefined, intro: undefined, headingLevel: "h2" },
);
</script>

<template>
  <section class="mx-auto max-w-5xl px-4 py-10 sm:px-6 sm:py-14">
    <component
      :is="headingLevel"
      v-if="title"
      class="text-2xl font-semibold tracking-tight sm:text-3xl"
    >
      {{ title }}
    </component>

    <p v-if="intro" class="mt-3 max-w-2xl text-text-muted">{{ intro }}</p>

    <div v-if="$slots.default" :class="title || intro ? 'mt-8' : ''">
      <slot />
    </div>
  </section>
</template>
