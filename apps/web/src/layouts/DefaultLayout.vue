<script setup lang="ts">
/**
 * -----------------------------------------------------------------------------
 * DefaultLayout
 * -----------------------------------------------------------------------------
 *
 * Standard application shell: header with product name, primary navigation,
 * session controls and theme control, plus the routed page area.
 *
 * Responsibilities:
 * - Provides the page chrome and the skip link required for keyboard users.
 * - Reflects the session (account link when signed in, sign-in link when not);
 *   all session STATE lives in the auth module's store.
 *
 * It holds no business logic and performs no data fetching. Navigation becomes
 * permission-aware in Fase 5, filtered by the permissions composable.
 */
import { useSessionStore } from "@enterprise/module-auth";
import { usePermissions } from "@enterprise/module-authorization";
import { NotificationBell } from "@enterprise/module-notifications";
import { computed } from "vue";
import ThemeToggle from "@/shared/components/ThemeToggle.vue";

const session = useSessionStore();
const { can } = usePermissions();

// Navigation is permission-aware: entries the user cannot open are not
// rendered at all, so the menu never leads to a forbidden page.
const navigation = computed(() =>
  [
    { label: "Demo", to: "/demo", permission: undefined },
    { label: "Users", to: "/users", permission: "users.read" },
  ].filter((item) => item.permission === undefined || can(item.permission)),
);
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

        <div class="ml-auto flex items-center gap-3">
          <NotificationBell v-if="session.isAuthenticated" />

          <RouterLink
            v-if="session.isAuthenticated"
            to="/account"
            class="rounded-md px-3 py-1.5 text-sm text-text-muted transition-colors hover:bg-background hover:text-text"
            active-class="bg-background text-text"
            data-testid="nav-account"
          >
            {{ session.user?.displayName ?? "Account" }}
          </RouterLink>
          <RouterLink
            v-else
            to="/login"
            class="rounded-md px-3 py-1.5 text-sm text-text-muted transition-colors hover:bg-background hover:text-text"
            data-testid="nav-sign-in"
          >
            Sign in
          </RouterLink>

          <ThemeToggle />
        </div>
      </div>
    </header>

    <main id="main-content" class="mx-auto max-w-5xl px-6 py-10">
      <slot />
    </main>
  </div>
</template>
