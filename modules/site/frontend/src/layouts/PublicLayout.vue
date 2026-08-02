<script setup lang="ts">
/**
 * -----------------------------------------------------------------------------
 * PublicLayout
 * -----------------------------------------------------------------------------
 *
 * Shell of the public site: header, routed page, footer.
 *
 * It is deliberately a different shell from `AdminLayout`, not a variant of
 * it: a marketing page and a work tool want opposite things from their
 * chrome. The private area keeps a dense header and a thumb-reachable bottom
 * bar; the public site gives the content the full width and puts navigation
 * out of the way.
 *
 * `data-surface="public"` marks the subtree for the token overrides that give
 * the site its own scale and rhythm (Fase 3 of
 * plans/public-and-admin-areas.md). Until those overrides exist the attribute
 * changes nothing — which is the point: the seam ships before the styling, so
 * adding it later touches one stylesheet instead of every page.
 */
import SiteFooter from "../components/SiteFooter.vue";
import SiteHeader from "../components/SiteHeader.vue";
</script>

<template>
  <div data-surface="public" class="flex min-h-dvh flex-col">
    <a
      href="#main-content"
      class="sr-only focus:not-sr-only focus:absolute focus:top-2 focus:left-2 focus:z-50 focus:rounded-(--radius-control) focus:bg-primary focus:px-3 focus:py-2 focus:text-on-primary"
    >
      Vai al contenuto
    </a>

    <SiteHeader />

    <main id="main-content" class="flex-1">
      <slot />
    </main>

    <SiteFooter />
  </div>
</template>
