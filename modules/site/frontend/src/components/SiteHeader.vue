<script setup lang="ts">
/**
 * -----------------------------------------------------------------------------
 * SiteHeader
 * -----------------------------------------------------------------------------
 *
 * Header of the public site: brand, primary navigation and the way in to the
 * private area.
 *
 * Responsibilities:
 * - Renders the public navigation, inline from `md` up and behind a
 *   disclosure below it.
 * - Shows "sign in" or "private area" depending on the session, which the
 *   HOST reports — this module never imports the Auth module.
 *
 * Responsive notes (docs/design-system.md §12):
 * - The menu opens on TAP, never on hover: on a touch screen hover does not
 *   exist, and a menu that needs it is simply unreachable.
 * - It closes on navigation. A panel left open over the page the user just
 *   asked for is the classic mobile-menu bug.
 * - `aria-expanded` and `aria-controls` are what make the button a real
 *   disclosure for a screen reader, rather than an unlabelled square.
 */
import { Bars3Icon, XMarkIcon } from "@heroicons/vue/24/outline";
import { ref, watch } from "vue";
import { useRoute } from "vue-router";
import { useSiteContent } from "../content";
import { useSiteLinks } from "../install";

const content = useSiteContent();
const links = useSiteLinks();
const route = useRoute();
const isOpen = ref(false);

const navigation = [
  { label: "Home", to: "/" },
  { label: content.about.title, to: "/about" },
  { label: content.services.title, to: "/services" },
  { label: content.contact.title, to: "/contact" },
];

watch(
  () => route.fullPath,
  () => {
    isOpen.value = false;
  },
);
</script>

<template>
  <header class="border-b border-border bg-surface">
    <div class="mx-auto flex max-w-5xl items-center gap-3 px-4 py-3 sm:px-6">
      <RouterLink to="/" class="truncate font-semibold tracking-tight">
        {{ content.name }}
      </RouterLink>

      <nav aria-label="Sito" class="ml-auto hidden items-center gap-1 md:flex">
        <RouterLink
          v-for="item in navigation"
          :key="item.to"
          :to="item.to"
          class="rounded-(--radius-control) px-3 py-1.5 text-sm text-text-muted transition-colors hover:bg-background hover:text-text"
          active-class="bg-background text-text"
        >
          {{ item.label }}
        </RouterLink>
      </nav>

      <div class="ml-auto flex shrink-0 items-center gap-2 md:ml-0">
        <RouterLink
          :to="links.entryPath()"
          class="rounded-(--radius-control) bg-primary px-3 py-2 text-sm font-medium text-on-primary transition-colors hover:bg-primary-hover"
          data-testid="site-entry"
        >
          {{ links.entryLabel() }}
        </RouterLink>

        <button
          type="button"
          class="rounded-(--radius-control) p-2 text-text-muted transition-colors hover:bg-background hover:text-text md:hidden"
          :aria-expanded="isOpen"
          aria-controls="site-menu"
          :aria-label="isOpen ? 'Chiudi il menu' : 'Apri il menu'"
          data-testid="site-menu-toggle"
          @click="isOpen = !isOpen"
        >
          <component :is="isOpen ? XMarkIcon : Bars3Icon" class="size-6" aria-hidden="true" />
        </button>
      </div>
    </div>

    <nav v-show="isOpen" id="site-menu" aria-label="Sito" class="border-t border-border md:hidden">
      <ul class="mx-auto max-w-5xl px-4 py-2 sm:px-6">
        <li v-for="item in navigation" :key="item.to">
          <RouterLink
            :to="item.to"
            class="flex min-h-11 items-center rounded-(--radius-control) px-2 text-text-muted transition-colors"
            active-class="text-primary"
          >
            {{ item.label }}
          </RouterLink>
        </li>
      </ul>
    </nav>
  </header>
</template>
