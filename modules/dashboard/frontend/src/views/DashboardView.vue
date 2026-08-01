<script setup lang="ts">
/**
 * -----------------------------------------------------------------------------
 * DashboardView
 * -----------------------------------------------------------------------------
 *
 * Landing page of the application: the page title and the widget grid.
 *
 * Deliberately thin — everything that varies lives in the grid and in the
 * widgets the server contributes.
 */
import DashboardGrid from "../components/DashboardGrid.vue";
</script>

<template>
  <div class="space-y-6">
    <header>
      <h1 class="text-2xl font-semibold tracking-tight">Dashboard</h1>
      <p class="mt-1 text-sm text-text-muted">An overview of the modules installed for you.</p>
    </header>

    <DashboardGrid />
  </div>
</template>
