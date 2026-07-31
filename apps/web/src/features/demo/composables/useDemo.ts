import { useMutation, useQuery, useQueryClient } from "@tanstack/vue-query";
import { demoApi } from "@/features/demo/api/demo.api";
import type { IEchoResponse, IPingResponse } from "@/features/demo/types/demo.types";

/**
 * Query keys of the Demo feature.
 *
 * Centralized so invalidation is never done with a hand-typed string: the
 * realtime layer (Fase 6) invalidates caches through these same keys.
 */
export const demoKeys = {
  all: ["demo"] as const,
  ping: () => [...demoKeys.all, "ping"] as const,
};

/**
 * Reads the ping endpoint.
 *
 * Server state, therefore TanStack Query — never a Pinia store. Caching,
 * retries and loading flags come from the query client defaults.
 *
 * @returns The query result: `data`, `isPending`, `isError`, `error`, `refetch`.
 */
export function usePing() {
  return useQuery<IPingResponse>({
    queryKey: demoKeys.ping(),
    queryFn: ({ signal }) => demoApi.ping(signal),
  });
}

/**
 * Sends a text to the echo endpoint.
 *
 * Side effect: on success it invalidates the ping query, demonstrating the
 * mutation → invalidation → automatic UI refresh cycle that the whole
 * framework relies on (and that SignalR events will trigger in Fase 6).
 *
 * @returns The mutation object: `mutate`, `mutateAsync`, `isPending`, `error`, `data`.
 */
export function useEcho() {
  const queryClient = useQueryClient();

  return useMutation<IEchoResponse, Error, string>({
    mutationFn: (text) => demoApi.echo(text),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: demoKeys.ping() }),
  });
}
