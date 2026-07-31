<script setup lang="ts">
/**
 * -----------------------------------------------------------------------------
 * ForbiddenPage
 * -----------------------------------------------------------------------------
 *
 * Shown when the permission guard refuses a navigation.
 *
 * It states plainly that this is a permissions matter and points at who can
 * fix it — a bare "403" leaves users guessing whether the app is broken.
 */
</script>

<template>
  <section class="py-16 text-center">
    <p class="text-sm font-medium text-text-muted">403</p>
    <h1 class="mt-2 text-2xl font-semibold tracking-tight">You do not have access to this page</h1>
    <p class="mt-2 text-text-muted">
      Your account is missing the required permission. Ask an administrator if you need it.
    </p>
    <RouterLink
      to="/"
      class="mt-6 inline-block rounded-md bg-primary px-4 py-2 text-sm font-medium text-on-primary transition-colors hover:bg-primary-hover"
    >
      Back to home
    </RouterLink>
  </section>
</template>
