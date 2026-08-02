<script setup lang="ts">
/**
 * -----------------------------------------------------------------------------
 * PrivacyPage
 * -----------------------------------------------------------------------------
 *
 * Public "privacy" page: heading, intro and the entries the project declared.
 *
 * A layout, not content: every word comes from `SiteContent.privacy`, so a
 * project rewrites the page by editing one object.
 */
import { useSiteContent } from "../content";
import HighlightGrid from "../components/HighlightGrid.vue";
import PageSection from "../components/PageSection.vue";

const content = useSiteContent();
</script>

<template>
  <PageSection :title="content.privacy.title" :intro="content.privacy.intro" heading-level="h1">
    <HighlightGrid :items="content.privacy.highlights" />
  </PageSection>
</template>
