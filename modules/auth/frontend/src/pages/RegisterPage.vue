<script setup lang="ts">
/**
 * -----------------------------------------------------------------------------
 * RegisterPage
 * -----------------------------------------------------------------------------
 *
 * Account creation form.
 *
 * Responsibilities:
 * - Client-side validation, including the shared password policy with hints.
 * - Delegates registration to the session store (registering signs in).
 * - Surfaces per-field server errors from `ValidationError` and the
 *   email-taken case from `BusinessError`.
 */
import { Button, Card, Input } from "@enterprise/ui";
import { ApplicationError, ValidationError } from "@enterprise/shared";
import { ref } from "vue";
import { useRouter } from "vue-router";
import { getHomePath } from "../home";
import { useSessionStore } from "../stores/session.store";
import { registerFormSchema } from "../validators/auth.validator";

const session = useSessionStore();
const router = useRouter();

const email = ref("");
const displayName = ref("");
const password = ref("");
const fieldErrors = ref<{
  email?: string | undefined;
  displayName?: string | undefined;
  password?: string | undefined;
}>({});
const formError = ref<string>();
const isSubmitting = ref(false);

/** Validates and submits the registration, then enters the app signed in. */
async function submit(): Promise<void> {
  fieldErrors.value = {};
  formError.value = undefined;

  const parsed = registerFormSchema.safeParse({
    email: email.value,
    displayName: displayName.value,
    password: password.value,
  });
  if (!parsed.success) {
    for (const issue of parsed.error.issues) {
      const field = issue.path[0];
      if (field === "email" || field === "displayName" || field === "password") {
        fieldErrors.value[field] ??= issue.message;
      }
    }
    return;
  }

  isSubmitting.value = true;
  try {
    await session.register(parsed.data);
    await router.replace(getHomePath());
  } catch (error) {
    if (error instanceof ValidationError) {
      // Server field names are PascalCase (FluentValidation property names).
      fieldErrors.value = {
        email: error.errors["Email"]?.[0],
        displayName: error.errors["DisplayName"]?.[0],
        password: error.errors["Password"]?.[0],
      };
      return;
    }
    formError.value =
      error instanceof ApplicationError ? error.message : "Something went wrong. Try again.";
  } finally {
    isSubmitting.value = false;
  }
}
</script>

<template>
  <Card title="Create your account" heading-level="h2" class="w-full max-w-sm">
    <form class="space-y-4" novalidate @submit.prevent="submit">
      <Input
        v-model="displayName"
        label="Display name"
        autocomplete="name"
        :error="fieldErrors.displayName"
        required
      />
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
        autocomplete="new-password"
        hint="At least 12 characters, with upper and lower case and a digit."
        :error="fieldErrors.password"
        required
      />

      <p v-if="formError" role="alert" class="text-sm text-danger">{{ formError }}</p>

      <Button type="submit" block :loading="isSubmitting">Create account</Button>

      <p class="text-center text-sm text-text-muted">
        Already registered?
        <RouterLink to="/login" class="font-medium text-primary hover:underline">
          Sign in
        </RouterLink>
      </p>
    </form>
  </Card>
</template>
