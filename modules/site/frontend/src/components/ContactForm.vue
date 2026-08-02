<script setup lang="ts">
/**
 * -----------------------------------------------------------------------------
 * ContactForm
 * -----------------------------------------------------------------------------
 *
 * The public contact form: name, email, message.
 *
 * Responsibilities:
 * - Validates client-side for fast feedback, then submits through the feature
 *   service. The server revalidates everything — a visitor can reach the
 *   endpoint without ever loading this page.
 * - Reports the three outcomes that matter: sent, rejected field, and "too
 *   many messages" (the endpoint's own rate limit).
 * - Carries the honeypot field that automated submitters fill.
 *
 * On success the form is REPLACED by the confirmation rather than cleared:
 * an empty form after submitting reads as "nothing happened", and the visitor
 * writes again.
 */
import { Button, Input, Textarea } from "@enterprise/ui";
import { RateLimitedError, ValidationError } from "@enterprise/shared";
import { ref } from "vue";
import { siteApi } from "../api/site.api";
import { contactFormSchema } from "../validators/contact.validator";

const name = ref("");
const email = ref("");
const body = ref("");

/**
 * Honeypot. Hidden from people and never focusable, so a real visitor cannot
 * fill it by accident — `aria-hidden` plus `tabindex="-1"` keep it away from
 * screen readers and the keyboard alike, which is what makes it fair.
 */
const website = ref("");

const fieldErrors = ref<{ name?: string; email?: string; body?: string }>({});
const formError = ref<string>();
const isSubmitting = ref(false);
const isSent = ref(false);

/** Validates and submits the message. */
async function submit(): Promise<void> {
  fieldErrors.value = {};
  formError.value = undefined;

  const parsed = contactFormSchema.safeParse({
    name: name.value,
    email: email.value,
    body: body.value,
  });

  if (!parsed.success) {
    for (const issue of parsed.error.issues) {
      const field = issue.path[0];
      if (field === "name" || field === "email" || field === "body") {
        fieldErrors.value[field] ??= issue.message;
      }
    }
    return;
  }

  isSubmitting.value = true;
  try {
    await siteApi.sendContactMessage({ ...parsed.data, website: website.value });
    isSent.value = true;
  } catch (error) {
    if (error instanceof ValidationError) {
      formError.value = "Controlla i dati inseriti e riprova.";
    } else if (error instanceof RateLimitedError) {
      // The endpoint's limit is deliberately tight, so this is a foreseeable
      // answer rather than an exceptional one: the remedy is waiting, and
      // saying so beats a generic failure the visitor would retry at once.
      formError.value = "Hai inviato troppi messaggi. Riprova fra qualche minuto.";
    } else {
      formError.value = "Non siamo riusciti a inviare il messaggio. Riprova più tardi.";
    }
  } finally {
    isSubmitting.value = false;
  }
}
</script>

<template>
  <p v-if="isSent" role="status" class="text-text" data-testid="contact-sent">
    Grazie, abbiamo ricevuto il tuo messaggio. Ti risponderemo al più presto.
  </p>

  <form v-else class="max-w-xl space-y-4" novalidate @submit.prevent="submit">
    <Input v-model="name" label="Nome" required autocomplete="name" :error="fieldErrors.name" />

    <Input
      v-model="email"
      label="Email"
      type="email"
      required
      autocomplete="email"
      :error="fieldErrors.email"
    />

    <Textarea
      v-model="body"
      label="Messaggio"
      required
      :maxlength="5000"
      :error="fieldErrors.body"
    />

    <!-- Honeypot: hidden from people, irresistible to bots. -->
    <div class="hidden" aria-hidden="true">
      <label for="site-contact-website">Sito web</label>
      <input id="site-contact-website" v-model="website" type="text" tabindex="-1" />
    </div>

    <p v-if="formError" role="alert" class="text-sm text-danger">{{ formError }}</p>

    <Button type="submit" :loading="isSubmitting">Invia</Button>
  </form>
</template>
