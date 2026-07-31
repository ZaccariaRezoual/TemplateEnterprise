import type { ApiClient, components } from "@enterprise/sdk";
import { executeSdkCall } from "@enterprise/shared";
import { defineStore } from "pinia";
import { computed, ref } from "vue";

/** A notification as returned by the API. */
export type Notification = components["schemas"]["NotificationDto"];

let configuredApi: ApiClient | undefined;

/**
 * Injects the application's SDK client. Called once by
 * `installNotificationsModule`.
 *
 * @param api The configured SDK client.
 */
export function provideNotificationsApi(api: ApiClient): void {
  configuredApi = api;
}

function requireApi(): ApiClient {
  if (configuredApi === undefined) {
    throw new Error(
      "Notifications module is not installed. Call installNotificationsModule() at bootstrap.",
    );
  }
  return configuredApi;
}

/**
 * The user's notification centre.
 *
 * Client state: it mirrors what the server holds but drives immediate UI
 * (the unread badge), so a background refetch must never make the badge
 * flicker mid-interaction — which is why this is Pinia and not a query cache.
 *
 * In Fase 6 the Realtime module will push new notifications straight into
 * `receive()`, and nothing else in this package changes.
 */
export const useNotificationsStore = defineStore("notifications", () => {
  const items = ref<Notification[]>([]);
  const isLoading = ref(false);

  /** Number of unread notifications, shown on the bell. */
  const unreadCount = computed(() => items.value.filter((item) => item.isUnread).length);

  /**
   * Loads the most recent notifications.
   * Failures are swallowed: a notification centre that cannot load must not
   * break the page it lives in.
   */
  async function load(): Promise<void> {
    isLoading.value = true;
    try {
      items.value = await executeSdkCall(() => requireApi().GET("/api/notifications"));
    } catch {
      items.value = [];
    } finally {
      isLoading.value = false;
    }
  }

  /**
   * Adds a notification received out of band (realtime push, Fase 6).
   * Ignores duplicates, since a push may race with a refetch.
   *
   * @param notification The notification to add.
   */
  function receive(notification: Notification): void {
    if (!items.value.some((item) => item.id === notification.id)) {
      items.value = [notification, ...items.value];
    }
  }

  /**
   * Marks one notification, or all of them, as read.
   *
   * The local state is updated first so the badge responds immediately; a
   * failed call leaves the server as the source of truth on the next load.
   *
   * @param notificationId The notification to mark, or undefined for all.
   */
  async function markAsRead(notificationId?: string): Promise<void> {
    items.value = items.value.map((item) =>
      notificationId === undefined || item.id === notificationId
        ? { ...item, isUnread: false }
        : item,
    );

    await executeSdkCall(() =>
      requireApi().POST("/api/notifications/read", {
        body: { notificationId: notificationId ?? null },
      }),
    ).catch(() => undefined);
  }

  /** Drops everything (called on sign-out). */
  function clear(): void {
    items.value = [];
  }

  return { items, isLoading, unreadCount, load, receive, markAsRead, clear };
});
