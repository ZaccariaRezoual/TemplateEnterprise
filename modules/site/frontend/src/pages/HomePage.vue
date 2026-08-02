<script setup lang="ts">
/**
 * -----------------------------------------------------------------------------
 * HomePage
 * -----------------------------------------------------------------------------
 *
 * Landing page of the public site: the claim, one action, and a summary of
 * what the organisation does.
 *
 * One primary action only. A hero offering three equally weighted buttons
 * asks the visitor to make a decision before they know anything — and the
 * usual result is that they make none.
 *
 * All of the words come from `SiteContent`: this file is the layout, the
 * project owns the message.
 */
import { useSiteContent } from "../content";
import HighlightGrid from "../components/HighlightGrid.vue";
import PageSection from "../components/PageSection.vue";

const content = useSiteContent();
</script>

<template>
  <PageSection :title="content.claim" :intro="content.intro" heading-level="h1">
    <RouterLink
      to="/contact"
      class="inline-flex min-h-11 items-center rounded-(--radius-control) bg-primary px-5 text-sm font-medium text-on-primary transition-colors hover:bg-primary-hover"
    >
      {{ content.callToAction }}
    </RouterLink>
  </PageSection>

  <div class="border-t border-border bg-surface">
    <PageSection :title="content.services.title" :intro="content.services.intro">
      <HighlightGrid :items="content.services.highlights" />
    </PageSection>
  </div>
</template>
