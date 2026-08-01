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
 * - Adapts the navigation to the viewport: a bottom bar on phones, inline in
 *   the header from `md` up.
 *
 * Responsive shape (see docs/design-system.md §13):
 * - The header holds only what must always be reachable. Packing brand, nav
 *   and every control into one row is what causes horizontal scrolling on a
 *   375px phone, and horizontal scrolling is the one layout bug users cannot
 *   work around.
 * - Primary navigation moves to a fixed bottom bar on small screens: it is
 *   within thumb reach, it is the platform-conventional place to look, and it
 *   keeps the header free for identity and status.
 * - `main` reserves bottom padding on small screens so content never ends up
 *   underneath that bar.
 *
 * It holds no business logic and performs no data fetching.
 */
import { useSessionStore } from "@enterprise/module-auth";
import { usePermissions } from "@enterprise/module-authorization";
import { NotificationBell } from "@enterprise/module-notifications";
import { ConnectionIndicator } from "@enterprise/module-realtime";
import { BeakerIcon, Squares2X2Icon, UsersIcon } from "@heroicons/vue/24/outline";
import { computed } from "vue";
import ThemeToggle from "@/shared/components/ThemeToggle.vue";

const session = useSessionStore();
const { can } = usePermissions();

// Navigation is permission-aware: entries the user cannot open are not
// rendered at all, so the menu never leads to a forbidden page.
// Icons are required, not decorative: a bottom bar with labels alone is hard
// to scan, and one with icons alone is hard to understand.
const navigation = computed(() =>
  [
    { label: "Dashboard", to: "/dashboard", icon: Squares2X2Icon, permission: undefined },
    { label: "Demo", to: "/demo", icon: BeakerIcon, permission: undefined },
    { label: "Users", to: "/users", icon: UsersIcon, permission: "users.read" },
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
      <div class="mx-auto flex max-w-5xl items-center gap-3 px-4 py-2 sm:px-6 sm:py-3 md:gap-6">
        <!-- Truncates rather than wrapping or pushing the row wider: a long
             product name must never be what breaks the layout. -->
        <span class="truncate font-semibold tracking-tight">Enterprise Framework</span>

        <!-- Inline navigation from `md` up; below that it lives in the bottom bar. -->
        <nav aria-label="Main" class="hidden items-center gap-1 md:flex">
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

        <div class="ml-auto flex shrink-0 items-center gap-1 sm:gap-3">
          <!-- Status detail, not an action: the first thing to give up space. -->
          <ConnectionIndicator v-if="session.isAuthenticated" class="hidden sm:flex" />

          <NotificationBell v-if="session.isAuthenticated" />

          <RouterLink
            v-if="session.isAuthenticated"
            to="/account"
            class="max-w-32 truncate rounded-md px-2 py-1.5 text-sm text-text-muted transition-colors hover:bg-background hover:text-text sm:px-3"
            active-class="bg-background text-text"
            data-testid="nav-account"
          >
            {{ session.user?.displayName ?? "Account" }}
          </RouterLink>
          <RouterLink
            v-else
            to="/login"
            class="rounded-md px-2 py-1.5 text-sm text-text-muted transition-colors hover:bg-background hover:text-text sm:px-3"
            data-testid="nav-sign-in"
          >
            Sign in
          </RouterLink>

          <ThemeToggle />
        </div>
      </div>
    </header>

    <!-- Bottom padding on small screens keeps the last element clear of the
         navigation bar; `pb-20` covers the bar plus the safe-area inset. -->
    <main id="main-content" class="mx-auto max-w-5xl px-4 py-6 pb-20 sm:px-6 sm:py-10 md:pb-10">
      <slot />
    </main>

    <!-- Primary navigation on phones. Fixed, thumb-reachable, and padded for
         the iOS home indicator so the last row of pixels stays tappable. -->
    <nav
      v-if="navigation.length > 0"
      aria-label="Main"
      class="fixed inset-x-0 bottom-0 z-40 border-t border-border bg-surface pb-[env(safe-area-inset-bottom)] md:hidden"
      data-testid="mobile-nav"
    >
      <ul class="flex items-stretch justify-around">
        <li v-for="item in navigation" :key="item.to" class="flex-1">
          <RouterLink
            :to="item.to"
            class="flex min-h-12 flex-col items-center justify-center gap-0.5 px-2 py-2 text-xs text-text-muted transition-colors"
            active-class="text-primary"
          >
            <component :is="item.icon" class="size-5" aria-hidden="true" />
            {{ item.label }}
          </RouterLink>
        </li>
      </ul>
    </nav>
  </div>
</template>
