<script setup lang="ts">
/**
 * -----------------------------------------------------------------------------
 * DefaultLayout
 * -----------------------------------------------------------------------------
 *
 * Standard application shell: header with product name, primary navigation and
 * theme control, plus the routed page area.
 *
 * Responsibilities:
 * - Provides the page chrome and the skip link required for keyboard users.
 * - Renders navigation entries; features contribute pages, not chrome.
 *
 * It holds no business logic and performs no data fetching. Navigation becomes
 * permission-aware in Fase 5, filtered by the permissions composable.
 */
import ThemeToggle from "@/shared/components/ThemeToggle.vue";

const navigation = [{ label: "Demo", to: "/demo" }] as const;
</script>

<template>
  <div class="min-h-dvh">
    <a
      href="#main-content"
      class="sr-only focus:not-sr-only focus:absolute focus:top-2 focus:left-2 focus:z-50 focus:rounded-md focus:bg-primary focus:px-3 focus:py-2 focus:text-on-primary"
    >
      Skip to content
    </a>

    <header class="border-b border-border bg-surface">
      <div class="mx-auto flex max-w-5xl items-center gap-6 px-6 py-3">
        <span class="font-semibold tracking-tight">Enterprise Framework</span>

        <nav aria-label="Main" class="flex items-center gap-1">
          <RouterLink
            v-for="item in navigation"
            :key="item.to"
            :to="item.to"
            class="rounded-md px-3 py-1.5 text-sm text-text-muted transition-colors hover:bg-background hover:text-text"
            active-class="bg-background text-text"
          >
            {{ item.label }}
          </RouterLink>
        </nav>

        <div class="ml-auto">
          <ThemeToggle />
        </div>
      </div>
    </header>

    <main id="main-content" class="mx-auto max-w-5xl px-6 py-10">
      <slot />
    </main>
  </div>
</template>
