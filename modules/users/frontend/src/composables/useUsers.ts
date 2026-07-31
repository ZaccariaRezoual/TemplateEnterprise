import { useQuery } from "@tanstack/vue-query";
import { computed, type Ref } from "vue";
import { usersApi, type UserProfilePage } from "../api/users.api";

/**
 * Query keys of the Users module. Centralized so cache invalidation — and the
 * realtime invalidation SignalR will trigger in Fase 6 — never relies on a
 * hand-typed string.
 */
export const usersKeys = {
  all: ["users"] as const,
  list: (page: number, search: string) => [...usersKeys.all, "list", page, search] as const,
};

/**
 * Reads the paged user list.
 *
 * Server state, so TanStack Query owns it. The query re-runs when page or
 * search change because the key includes them.
 *
 * @param page Reactive 1-based page index.
 * @param search Reactive search term.
 * @returns The query result: `data`, `isPending`, `isError`, `error`.
 */
export function useUsersList(page: Ref<number>, search: Ref<string>) {
  return useQuery<UserProfilePage>({
    queryKey: computed(() => usersKeys.list(page.value, search.value)),
    queryFn: ({ signal }) =>
      usersApi.list(
        {
          page: page.value,
          pageSize: 25,
          ...(search.value === "" ? {} : { search: search.value }),
        },
        signal,
      ),
  });
}
