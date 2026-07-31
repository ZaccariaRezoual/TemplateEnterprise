<script setup lang="ts">
/**
 * -----------------------------------------------------------------------------
 * AccountPage
 * -----------------------------------------------------------------------------
 *
 * Profile of the signed-in user; the module's demonstration of a PROTECTED
 * route (meta.requiresAuth + guard).
 *
 * The profile is re-fetched from the Bearer-protected `/me` endpoint instead
 * of only trusting the session store, proving the token-attach flow
 * end-to-end on every visit.
 */
import { Avatar, Badge, Button, Card } from "@enterprise/ui";
import { ApplicationError } from "@enterprise/shared";
import { onMounted, ref } from "vue";
import { useRouter } from "vue-router";
import type { AuthUser } from "../api/auth.api";
import { useSessionStore } from "../stores/session.store";

const session = useSessionStore();
const router = useRouter();

const profile = ref<AuthUser | undefined>(session.user);
const error = ref<string>();

onMounted(async () => {
  try {
    profile.value = await session.fetchProfile();
  } catch (loadError) {
    error.value =
      loadError instanceof ApplicationError ? loadError.message : "Could not load the profile.";
  }
});

/** Ends the session and returns to the sign-in page. */
async function signOut(): Promise<void> {
  await session.logout();
  await router.replace({ name: "login" });
}
</script>

<template>
  <div class="mx-auto max-w-xl space-y-6">
    <Card title="Account" heading-level="h2">
      <template #actions>
        <Button variant="secondary" size="sm" data-testid="sign-out" @click="signOut">
          Sign out
        </Button>
      </template>

      <p v-if="error" role="alert" class="text-sm text-danger">{{ error }}</p>

      <div v-else-if="profile" class="flex items-center gap-4">
        <Avatar :name="profile.displayName" size="lg" decorative />
        <div class="min-w-0">
          <p class="truncate font-medium">{{ profile.displayName }}</p>
          <p class="truncate text-sm text-text-muted" data-testid="account-email">
            {{ profile.email }}
          </p>
          <div class="mt-1.5 flex gap-1.5">
            <Badge v-for="role in profile.roles" :key="role" variant="info">{{ role }}</Badge>
          </div>
        </div>
      </div>
    </Card>
  </div>
</template>
