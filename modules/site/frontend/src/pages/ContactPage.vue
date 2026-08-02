<script setup lang="ts">
/**
 * -----------------------------------------------------------------------------
 * ContactPage
 * -----------------------------------------------------------------------------
 *
 * Public contact page.
 *
 * Shows the form when the host wired an API client, and the plain address
 * otherwise: the public site also works as pages only, and a form that cannot
 * submit is worse than no form — the visitor believes they have written.
 *
 * The address stays visible either way. Some people would rather write from
 * their own mail client, and taking that away to force a form is a choice
 * made for our convenience, not theirs.
 */
import { computed } from "vue";
import { isContactAvailable } from "../api/site.api";
import { useSiteContent } from "../content";
import ContactForm from "../components/ContactForm.vue";
import PageSection from "../components/PageSection.vue";

const content = useSiteContent();
const canSubmit = computed(() => isContactAvailable());
</script>

<template>
  <PageSection :title="content.contact.title" :intro="content.contact.intro" heading-level="h1">
    <ContactForm v-if="canSubmit" class="mb-10" />

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
