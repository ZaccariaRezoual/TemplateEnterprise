<script setup lang="ts">
/**
 * -----------------------------------------------------------------------------
 * UsersPage
 * -----------------------------------------------------------------------------
 *
 * Administration list of user profiles.
 *
 * Responsibilities:
 * - Reads the paged list through the module's composable.
 * - Renders search, results and paging.
 * - Shows a clear "not permitted" state instead of an empty table when the
 *   user lacks `users.read` — an empty table would read as "no users exist".
 *
 * The route is already permission-guarded; this check is the second, visible
 * layer. The API is the one that actually enforces it.
 */
import { Badge, Card, Input } from "@enterprise/ui";
import { usePermissions } from "@enterprise/module-authorization";
import { formatDate } from "@enterprise/shared";
import { ref, watch } from "vue";
import { useUsersList } from "../composables/useUsers";

const { can } = usePermissions();

const page = ref(1);
const search = ref("");
const users = useUsersList(page, search);

// A new search must restart from the first page, or a user searching from
// page 3 sees "no results" for a term that has plenty.
watch(search, () => {
  page.value = 1;
});
</script>

<template>
  <div class="space-y-6">
    <header>
      <h1 class="text-2xl font-semibold tracking-tight">Users</h1>
      <p class="mt-1 text-text-muted">
        Accounts known to the platform, projected from the Auth module's events.
      </p>
    </header>

    <p v-if="!can('users.read')" role="alert" class="text-sm text-danger">
      You do not have permission to view users.
    </p>

    <template v-else>
      <Input v-model="search" label="Search" placeholder="Email or name…" label-hidden />

      <Card flush>
        <p v-if="users.isPending.value" class="p-5 text-sm text-text-muted">Loading…</p>

        <p v-else-if="users.isError.value" role="alert" class="p-5 text-sm text-danger">
          {{ users.error.value?.message }}
        </p>

        <div v-else-if="users.data.value" class="overflow-x-auto">
          <table class="w-full text-left text-sm">
            <caption class="sr-only">
              User accounts
            </caption>
            <thead class="border-b border-border text-text-muted">
              <tr>
                <th scope="col" class="px-5 py-3 font-medium">Name</th>
                <th scope="col" class="px-5 py-3 font-medium">Email</th>
                <th scope="col" class="px-5 py-3 font-medium">Status</th>
                <th scope="col" class="px-5 py-3 font-medium">Created</th>
              </tr>
            </thead>
            <tbody>
              <tr
                v-for="user in users.data.value.items"
                :key="user.id"
                class="border-b border-border last:border-0"
              >
                <td class="px-5 py-3 font-medium">{{ user.displayName }}</td>
                <td class="px-5 py-3 text-text-muted">{{ user.email }}</td>
                <td class="px-5 py-3">
                  <Badge :variant="user.isActive ? 'success' : 'neutral'">
                    {{ user.isActive ? "Active" : "Inactive" }}
                  </Badge>
                </td>
                <td class="px-5 py-3 text-text-muted tabular-nums">
                  {{ formatDate(user.createdAtUtc) }}
                </td>
              </tr>
              <tr v-if="users.data.value.items.length === 0">
                <td colspan="4" class="px-5 py-8 text-center text-text-muted">
                  No users match this search.
                </td>
              </tr>
            </tbody>
          </table>
        </div>
      </Card>

      <p v-if="users.data.value" class="text-sm text-text-muted" data-testid="users-total">
        {{ users.data.value.totalCount }} user(s)
      </p>
    </template>
  </div>
</template>
