import type { ApiClient, components } from "@enterprise/sdk";
import { executeSdkCall } from "@enterprise/shared";
import { defineStore } from "pinia";
import { computed, ref } from "vue";

/** Effective authorization of the signed-in user. */
export type UserAuthorization = components["schemas"]["UserAuthorizationDto"];

let configuredApi: ApiClient | undefined;

/**
 * Injects the application's SDK client. Called once by
 * `installAuthorizationModule`; stores cannot take constructor arguments, so
 * the dependency arrives through this seam.
 *
 * @param api The configured SDK client.
 */
export function provideAuthorizationApi(api: ApiClient): void {
  configuredApi = api;
}

/**
 * Permissions of the signed-in user.
 *
 * This is a UI CONVENIENCE, never a security boundary: it decides what to
 * render, while the API decides what may actually happen. Treating it as
 * security would mean anyone with devtools could grant themselves anything.
 *
 * Client state (who am I right now), so Pinia is its home — and it is
 * deliberately not cached in TanStack Query, whose refetching would make
 * "what can I do" flicker mid-interaction.
 */
export const usePermissionsStore = defineStore("authorization-permissions", () => {
  const roles = ref<readonly string[]>([]);
  const permissions = ref<readonly string[]>([]);

  /** Whether the permission set has been loaded for the current session. */
  const isLoaded = ref(false);

  /** Fast membership checks; rebuilt only when the permission list changes. */
  const permissionSet = computed(() => new Set(permissions.value));

  /**
   * Tells whether the user holds a permission.
   *
   * @param permission Permission name, e.g. "users.read".
   * @returns Whether the permission is granted.
   */
  function can(permission: string): boolean {
    return permissionSet.value.has(permission);
  }

  /**
   * Tells whether the user holds at least one of the given permissions.
   *
   * @param candidates Permission names.
   * @returns Whether any of them is granted.
   */
  function canAny(candidates: readonly string[]): boolean {
    return candidates.some(can);
  }

  /**
   * Loads the caller's roles and permissions from the API.
   * Safe to call when signed out: it clears the state instead of failing.
   */
  async function load(): Promise<void> {
    if (configuredApi === undefined) {
      throw new Error(
        "Authorization module is not installed. Call installAuthorizationModule() at bootstrap.",
      );
    }

    try {
      const result = await executeSdkCall(() => configuredApi!.GET("/api/authorization/me"));
      roles.value = result.roles;
      permissions.value = result.permissions;
      isLoaded.value = true;
    } catch {
      // Anonymous or expired session: no permissions, and the router guard
      // is what redirects — this store never navigates.
      clear();
    }
  }

  /** Drops the permission set (called on sign-out). */
  function clear(): void {
    roles.value = [];
    permissions.value = [];
    isLoaded.value = false;
  }

  return { roles, permissions, isLoaded, can, canAny, load, clear };
});
