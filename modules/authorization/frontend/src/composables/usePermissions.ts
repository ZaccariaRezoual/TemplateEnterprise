import { computed, type ComputedRef } from "vue";
import { usePermissionsStore } from "../stores/permissions.store";

/**
 * Permission checks for components.
 *
 * Use it to decide what to RENDER — an action the user cannot perform is
 * better hidden than shown and rejected. It is not a security mechanism: the
 * API enforces every permission independently.
 *
 * @returns Reactive helpers: `can`, `canAny`, plus the raw roles and permissions.
 *
 * @example
 * ```vue
 * const { can } = usePermissions();
 * <Button v-if="can('users.write')" @click="edit">Edit</Button>
 * ```
 */
export function usePermissions(): {
  can: (permission: string) => boolean;
  canAny: (permissions: readonly string[]) => boolean;
  roles: ComputedRef<readonly string[]>;
  permissions: ComputedRef<readonly string[]>;
  isLoaded: ComputedRef<boolean>;
} {
  const store = usePermissionsStore();

  return {
    can: (permission: string) => store.can(permission),
    canAny: (permissions: readonly string[]) => store.canAny(permissions),
    roles: computed(() => store.roles),
    permissions: computed(() => store.permissions),
    isLoaded: computed(() => store.isLoaded),
  };
}
