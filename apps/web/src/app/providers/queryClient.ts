import { QueryClient } from "@tanstack/vue-query";
import { ForbiddenError, NotFoundError, UnauthorizedError } from "@enterprise/shared";

/**
 * Builds the TanStack Query client that owns ALL server state.
 *
 * Defaults encode two framework rules:
 * - Retrying a request the server already rejected on purpose (401/403/404) is
 *   pointless and delays the error reaching the UI, so those are never retried.
 * - Mutations are not retried automatically: they are usually not idempotent.
 *
 * @returns A configured query client, one per application instance (tests
 * create their own to keep caches isolated).
 */
export function createQueryClient(): QueryClient {
  return new QueryClient({
    defaultOptions: {
      queries: {
        staleTime: 30_000,
        retry: (failureCount, error) => {
          if (
            error instanceof UnauthorizedError ||
            error instanceof ForbiddenError ||
            error instanceof NotFoundError
          ) {
            return false;
          }
          return failureCount < 2;
        },
      },
      mutations: { retry: false },
    },
  });
}
