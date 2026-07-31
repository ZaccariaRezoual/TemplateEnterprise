import { useRealtimeEvent } from "@enterprise/module-realtime";
import { useToast } from "@enterprise/ui";
import { useNotificationsStore, type Notification } from "../stores/notifications.store";

/** Shape the server pushes on the "notification.created" channel. */
interface INotificationCreated {
  id: string;
  title: string;
  body: string;
  level: string;
  link: string | null;
  createdAtUtc: string;
}

const TOAST_VARIANTS: Record<string, "info" | "success" | "warning" | "danger"> = {
  Info: "info",
  Success: "success",
  Warning: "warning",
  Error: "danger",
};

/**
 * Makes the notification centre live.
 *
 * Call once, from a component that lives for the whole session (the bell).
 * It subscribes to the realtime channel, pushes the notification into the
 * store — so the badge updates without a refetch — and raises a toast so the
 * user notices something arrived without watching the bell.
 *
 * The module knows nothing about SignalR: it subscribes to a CHANNEL through
 * the realtime composable, which is the only thing it depends on.
 */
export function useLiveNotifications(): void {
  const notifications = useNotificationsStore();
  const { show } = useToast();

  useRealtimeEvent<INotificationCreated>("notification.created", (payload) => {
    const notification: Notification = {
      id: payload.id,
      title: payload.title,
      body: payload.body,
      level: payload.level,
      link: payload.link,
      // Pushed notifications are unread by definition: the server only sends
      // them at creation time.
      isUnread: true,
      createdAtUtc: payload.createdAtUtc,
    };

    notifications.receive(notification);

    show({
      title: payload.title,
      description: payload.body,
      variant: TOAST_VARIANTS[payload.level] ?? "info",
    });
  });
}
