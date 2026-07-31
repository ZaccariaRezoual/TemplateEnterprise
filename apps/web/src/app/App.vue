<script setup lang="ts">
/**
 * -----------------------------------------------------------------------------
 * App
 * -----------------------------------------------------------------------------
 *
 * Root component of the application.
 *
 * Responsibilities:
 * - Selects the layout declared by the active route (`meta.layout`).
 * - Renders the routed page inside it.
 * - Hosts the toast stack, which must sit above every layout and survive
 *   navigation (a toast raised by a redirecting action must still be seen).
 *
 * It holds no business logic and no data fetching: those belong to features.
 */
import { ToastHost } from "@enterprise/ui";
import { computed } from "vue";
import { useRoute } from "vue-router";
import BlankLayout from "@/layouts/BlankLayout.vue";
import DefaultLayout from "@/layouts/DefaultLayout.vue";

const route = useRoute();

const layout = computed(() => (route.meta.layout === "blank" ? BlankLayout : DefaultLayout));
</script>

<template>
  <component :is="layout">
    <RouterView v-slot="{ Component }">
      <component :is="Component" />
    </RouterView>
  </component>

  <ToastHost />
</template>
