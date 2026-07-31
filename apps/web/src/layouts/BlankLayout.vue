<script setup lang="ts">
/**
 * -----------------------------------------------------------------------------
 * BlankLayout
 * -----------------------------------------------------------------------------
 *
 * Chrome-free layout that centers its content: used by sign-in, onboarding and
 * error screens, where navigation would be a distraction or is not yet
 * available. Opt in with `meta: { layout: "blank" }` on the route.
 */
</script>

<template>
  <main class="grid min-h-dvh place-items-center p-6">
    <slot />
  </main>
</template>
