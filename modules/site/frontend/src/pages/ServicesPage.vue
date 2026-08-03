<script setup lang="ts">
/**
 * -----------------------------------------------------------------------------
 * ServicesPage
 * -----------------------------------------------------------------------------
 *
 * Public "services" page: heading, intro and the entries the project declared.
 *
 * A layout, not content: every word comes from `SiteContent.services`, so a
 * project rewrites the page by editing one object.
 */
import { useSiteContent } from "../content";
import HighlightGrid from "../components/HighlightGrid.vue";
import { PageSection } from "@enterprise/ui";

const content = useSiteContent();
</script>

<template>
  <PageSection :title="content.services.title" :intro="content.services.intro" heading-level="h1">
    <HighlightGrid :items="content.services.highlights" />
  </PageSection>
</template>
