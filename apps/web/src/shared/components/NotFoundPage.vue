<script setup lang="ts">
/**
 * -----------------------------------------------------------------------------
 * NotFoundPage
 * -----------------------------------------------------------------------------
 *
 * Page rendered by the catch-all route when no other route matches.
 * Offers a way back instead of leaving the user at a dead end.
 */
</script>

<template>
  <section class="py-16 text-center">
    <p class="text-sm font-medium text-text-muted">404</p>
    <h1 class="mt-2 text-2xl font-semibold tracking-tight">This page does not exist</h1>
    <p class="mt-2 text-text-muted">The link may be outdated or the address mistyped.</p>
    <RouterLink
      to="/"
      class="mt-6 inline-block rounded-md bg-primary px-4 py-2 text-sm font-medium text-on-primary transition-colors hover:bg-primary-hover"
    >
      Back to home
    </RouterLink>
  </section>
</template>
