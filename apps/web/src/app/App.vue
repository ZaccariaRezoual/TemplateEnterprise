<script setup lang="ts">
/**
 * -----------------------------------------------------------------------------
 * App
 * -----------------------------------------------------------------------------
 *
 * Root component of the application.
 *
 * Responsibilities:
 * - Selects the shell of the active route: the public site marks its routes
 *   with `meta.publicSite`, everything else uses `meta.layout` and defaults
 *   to the administrative shell.
 * - Renders the routed page inside it.
 * - Hosts the toast stack, which must sit above every layout and survive
 *   navigation (a toast raised by a redirecting action must still be seen).
 *
 * It holds no business logic and no data fetching: those belong to features.
 */
import { PublicLayout } from "@enterprise/module-site";
import { ToastHost } from "@enterprise/ui";
import { computed } from "vue";
import { useRoute } from "vue-router";
import BlankLayout from "@/layouts/BlankLayout.vue";
import AdminLayout from "@/layouts/AdminLayout.vue";

const route = useRoute();

const layout = computed(() => {
  if (route.meta.publicSite === true) return PublicLayout;
  return route.meta.layout === "blank" ? BlankLayout : AdminLayout;
});
</script>

<template>
  <component :is="layout">
    <RouterView v-slot="{ Component }">
      <component :is="Component" />
    </RouterView>
  </component>

  <ToastHost />
</template>
