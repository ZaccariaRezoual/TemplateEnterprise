<script setup lang="ts">
/**
 * -----------------------------------------------------------------------------
 * SiteFooter
 * -----------------------------------------------------------------------------
 *
 * Footer of the public site: contact details, the privacy notice and the
 * copyright line.
 *
 * The privacy link is not decoration: a site that collects a name and an
 * email needs a reachable notice, and putting it here means every page has
 * one. The TEXT is the project's responsibility — the template only makes
 * sure there is a place for it.
 *
 * `env(safe-area-inset-bottom)` keeps the last row clear of the iOS home
 * indicator, per docs/design-system.md §12.
 */
import { useSiteContent } from "../content";

const content = useSiteContent();
const year = new Date().getFullYear();
</script>

<template>
  <footer class="border-t border-border bg-surface pb-[env(safe-area-inset-bottom)]">
    <div
      class="mx-auto flex max-w-5xl flex-col gap-4 px-4 py-8 text-sm sm:px-6 md:flex-row md:items-start md:justify-between"
    >
      <div class="min-w-0">
        <p class="font-medium">{{ content.name }}</p>
        <p class="mt-1 text-text-muted">
          <a :href="`mailto:${content.contact.email}`" class="hover:underline">
            {{ content.contact.email }}
          </a>
        </p>
        <p v-if="content.contact.address" class="mt-1 text-text-muted">
          {{ content.contact.address }}
        </p>
      </div>

      <nav aria-label="Legale" class="flex flex-col gap-2">
        <RouterLink to="/privacy" class="text-text-muted hover:text-text hover:underline">
          {{ content.privacy.title }}
        </RouterLink>
      </nav>
    </div>

    <p class="mx-auto max-w-5xl px-4 pb-6 text-xs text-text-muted sm:px-6">
      © {{ year }} {{ content.name }}
    </p>
  </footer>
</template>
