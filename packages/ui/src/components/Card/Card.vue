<script setup lang="ts">
/**
 * -----------------------------------------------------------------------------
 * Card
 * -----------------------------------------------------------------------------
 *
 * Surface that groups related content.
 *
 * Responsibilities:
 * - Provides the surface, border and spacing from semantic tokens.
 * - Renders an optional header (title, description, actions) and footer.
 *
 * It is a layout primitive: it holds no state and no logic. The heading level
 * is configurable so a card never breaks the page's heading outline — a real
 * accessibility failure that visual-only components tend to introduce.
 */
import { computed, useId } from "vue";
import type { CardProps } from "./Card.types";

const props = withDefaults(defineProps<CardProps>(), {
  flush: false,
  headingLevel: "h3",
});

const titleId = useId();
const hasTitle = computed(() => props.title !== undefined && props.title !== "");
const bodyPadding = computed(() => (props.flush ? "" : "px-5 pb-5"));
</script>

<template>
  <section
    class="rounded-(--card-radius) border border-border bg-surface"
    :aria-labelledby="hasTitle ? titleId : undefined"
  >
    <header v-if="hasTitle || $slots.header || $slots.actions" class="flex gap-4 p-5">
      <div class="min-w-0 flex-1">
        <slot name="header">
          <component :is="headingLevel" v-if="hasTitle" :id="titleId" class="font-medium">
            {{ title }}
          </component>
          <p v-if="description" class="mt-1 text-sm text-text-muted">{{ description }}</p>
        </slot>
      </div>
      <div v-if="$slots.actions" class="shrink-0">
        <slot name="actions" />
      </div>
    </header>

    <div :class="[bodyPadding, !(hasTitle || $slots.header) && !flush && 'pt-5']">
      <slot />
    </div>

    <footer v-if="$slots.footer" class="border-t border-border px-5 py-4">
      <slot name="footer" />
    </footer>
  </section>
</template>
