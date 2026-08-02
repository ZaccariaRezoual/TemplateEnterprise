<script setup lang="ts">
/**
 * -----------------------------------------------------------------------------
 * ContactPage
 * -----------------------------------------------------------------------------
 *
 * Public contact page.
 *
 * For now it publishes the address and the email: the form arrives with its
 * backend in Fase 2 of plans/public-and-admin-areas.md. Shipping the form
 * first would mean a page that looks finished and silently drops every
 * message — worse than no form at all, because the visitor believes they have
 * written to us.
 */
import { useSiteContent } from "../content";
import PageSection from "../components/PageSection.vue";

const content = useSiteContent();
</script>

<template>
  <PageSection :title="content.contact.title" :intro="content.contact.intro" heading-level="h1">
    <dl class="space-y-4 text-sm">
      <div>
        <dt class="font-medium">Email</dt>
        <dd class="mt-1">
          <a
            :href="`mailto:${content.contact.email}`"
            class="text-primary hover:underline"
            data-testid="contact-email"
          >
            {{ content.contact.email }}
          </a>
        </dd>
      </div>

      <div v-if="content.contact.address">
        <dt class="font-medium">Indirizzo</dt>
        <dd class="mt-1 text-text-muted">{{ content.contact.address }}</dd>
      </div>
    </dl>
  </PageSection>
</template>
