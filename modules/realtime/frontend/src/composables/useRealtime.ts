import { useQueryClient } from "@tanstack/vue-query";
import { computed, onScopeDispose, type ComputedRef } from "vue";
import { realtimeService } from "../RealtimeService";
import type { RealtimeHandler, RealtimeStatus } from "../types";

/**
 * Connection state, for status indicators.
 *
 * @returns The current status and whether the connection is usable.
 */
export function useRealtime(): {
  status: ComputedRef<RealtimeStatus>;
  isConnected: ComputedRef<boolean>;
  lastEventAt: ComputedRef<Date | undefined>;
} {
  return {
    status: computed(() => realtimeService.status.value),
    isConnected: computed(() => realtimeService.status.value === "connected"),
    lastEventAt: computed(() => realtimeService.lastEventAt.value),
  };
}

/**
 * Subscribes to a realtime channel for the lifetime of the calling scope.
 *
 * The subscription is removed automatically when the component unmounts —
 * doing it by hand is the classic source of handlers that keep firing against
 * a destroyed component after a few navigations.
 *
 * @param channel Channel name, e.g. "notification.created".
 * @param handler Called with each payload.
 *
 * @example
 * ```ts
 * useRealtimeEvent<INotification>("notification.created", (n) => store.receive(n));
 * ```
 */
export function useRealtimeEvent<TPayload>(
  channel: string,
  handler: RealtimeHandler<TPayload>,
): void {
  const unsubscribe = realtimeService.on(channel, handler);
  onScopeDispose(unsubscribe);
}

/**
 * Invalidates TanStack Query caches when a realtime event arrives.
 *
 * This is the whole point of the realtime layer for ordinary features: a
 * table updates because the server said something changed, without polling
 * and without the feature knowing SignalR exists. Invalidating rather than
 * patching the cache keeps the server authoritative — the refetch returns
 * what the user is actually allowed to see.
 *
 * @param channel Channel to listen on.
 * @param queryKey Key (or key prefix) to invalidate.
 *
 * @example
 * ```ts
 * useRealtimeInvalidation("user.updated", ["users"]);
 * ```
 */
export function useRealtimeInvalidation(channel: string, queryKey: readonly unknown[]): void {
  const queryClient = useQueryClient();

  useRealtimeEvent(channel, () => {
    void queryClient.invalidateQueries({ queryKey: [...queryKey] });
  });
}
