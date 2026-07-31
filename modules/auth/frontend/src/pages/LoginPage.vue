<script setup lang="ts">
/**
 * -----------------------------------------------------------------------------
 * LoginPage
 * -----------------------------------------------------------------------------
 *
 * Sign-in form.
 *
 * Responsibilities:
 * - Client-side validation for fast feedback (schema mirrors the backend).
 * - Delegates authentication to the session store.
 * - On success, returns the user to the route that redirected here (guard's
 *   `redirect` query) or home.
 *
 * Wrong credentials surface as a single generic message — the API is
 * deliberately unable to distinguish unknown email from wrong password.
 */
import { Button, Card, Input } from "@enterprise/ui";
import { ApplicationError } from "@enterprise/shared";
import { ref } from "vue";
import { useRoute, useRouter } from "vue-router";
import { useSessionStore } from "../stores/session.store";
import { loginFormSchema } from "../validators/auth.validator";

const session = useSessionStore();
const router = useRouter();
const route = useRoute();

const email = ref("");
const password = ref("");
const fieldErrors = ref<{ email?: string; password?: string }>({});
const formError = ref<string>();
const isSubmitting = ref(false);

/**
 * Validates and submits the credentials, then navigates back to the
 * requested destination.
 */
async function submit(): Promise<void> {
  fieldErrors.value = {};
  formError.value = undefined;

  const parsed = loginFormSchema.safeParse({ email: email.value, password: password.value });
  if (!parsed.success) {
    for (const issue of parsed.error.issues) {
      const field = issue.path[0];
      if (field === "email" || field === "password") {
        fieldErrors.value[field] ??= issue.message;
      }
    }
    return;
  }

  isSubmitting.value = true;
  try {
    await session.login(parsed.data);
    const redirect = typeof route.query["redirect"] === "string" ? route.query["redirect"] : "/";
    await router.replace(redirect);
  } catch (error) {
    formError.value =
      error instanceof ApplicationError ? error.message : "Something went wrong. Try again.";
  } finally {
    isSubmitting.value = false;
  }
}
</script>

<template>
  <Card title="Sign in" heading-level="h2" class="w-full max-w-sm">
    <form class="space-y-4" novalidate @submit.prevent="submit">
      <Input
        v-model="email"
        label="Email"
        type="email"
        autocomplete="email"
        :error="fieldErrors.email"
        required
      />
      <Input
        v-model="password"
        label="Password"
        type="password"
        autocomplete="current-password"
        :error="fieldErrors.password"
        required
      />

      <p v-if="formError" role="alert" class="text-sm text-danger">{{ formError }}</p>

      <Button type="submit" block :loading="isSubmitting">Sign in</Button>

      <p class="text-center text-sm text-text-muted">
        No account yet?
        <RouterLink to="/register" class="font-medium text-primary hover:underline">
          Create one
        </RouterLink>
      </p>
    </form>
  </Card>
</template>
