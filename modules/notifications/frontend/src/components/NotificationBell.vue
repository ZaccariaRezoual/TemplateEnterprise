<script setup lang="ts">
/**
 * -----------------------------------------------------------------------------
 * NotificationBell
 * -----------------------------------------------------------------------------
 *
 * Header control showing the unread count and, when opened, the notification
 * centre.
 *
 * Responsibilities:
 * - Renders the unread badge and the panel.
 * - Delegates every state change to the notifications store.
 *
 * Accessibility notes that are easy to get wrong here: the badge count is in
 * the button's accessible name (a bare number is meaningless to a screen
 * reader), the panel is a labelled region announced politely, and Escape
 * closes it because a keyboard user must be able to leave without a mouse.
 */
import { BellIcon } from "@heroicons/vue/24/outline";
import { Badge, Button } from "@enterprise/ui";
import { formatDate } from "@enterprise/shared";
import { onMounted, ref } from "vue";
import { useLiveNotifications } from "../composables/useLiveNotifications";
import { useNotificationsStore } from "../stores/notifications.store";

const notifications = useNotificationsStore();
const isOpen = ref(false);

// The bell lives for the whole session, so it is the right place to hold the
// realtime subscription: the badge then updates without anyone refetching.
useLiveNotifications();

onMounted(() => {
  void notifications.load();
});

/** Opens or closes the panel, loading fresh content when opening. */
function toggle(): void {
  isOpen.value = !isOpen.value;
  if (isOpen.value) {
    void notifications.load();
  }
}

const LEVEL_VARIANTS: Record<string, "info" | "success" | "warning" | "danger"> = {
  Info: "info",
  Success: "success",
  Warning: "warning",
  Error: "danger",
};
</script>

<template>
  <div class="relative">
    <button
      type="button"
      class="relative cursor-pointer rounded-md p-2 text-text-muted transition-colors hover:bg-background hover:text-text"
      :aria-expanded="isOpen"
      aria-haspopup="true"
      :aria-label="
        notifications.unreadCount > 0
          ? `Notifications, ${notifications.unreadCount} unread`
          : 'Notifications'
      "
      data-testid="notification-bell"
      @click="toggle"
    >
      <BellIcon class="size-5" aria-hidden="true" />
      <span
        v-if="notifications.unreadCount > 0"
        class="absolute -top-0.5 -right-0.5 grid min-w-4 place-items-center rounded-full bg-danger px-1 text-[10px] font-medium text-on-status"
        aria-hidden="true"
        data-testid="notification-badge"
      >
        {{ notifications.unreadCount > 9 ? "9+" : notifications.unreadCount }}
      </span>
    </button>

    <div
      v-if="isOpen"
      role="region"
      aria-label="Notifications"
      class="absolute right-0 z-50 mt-2 w-80 rounded-xl border border-border bg-surface shadow-lg"
      @keydown.esc="isOpen = false"
    >
      <div class="flex items-center justify-between gap-3 border-b border-border px-4 py-3">
        <h2 class="text-sm font-medium">Notifications</h2>
        <Button
          v-if="notifications.unreadCount > 0"
          variant="ghost"
          size="sm"
          data-testid="mark-all-read"
          @click="notifications.markAsRead()"
        >
          Mark all read
        </Button>
      </div>

      <p v-if="notifications.isLoading" class="px-4 py-6 text-sm text-text-muted">Loading…</p>

      <p
        v-else-if="notifications.items.length === 0"
        class="px-4 py-6 text-sm text-text-muted"
        data-testid="notifications-empty"
      >
        You have no notifications.
      </p>

      <ul v-else class="max-h-96 overflow-y-auto">
        <li
          v-for="item in notifications.items"
          :key="item.id"
          class="border-b border-border px-4 py-3 last:border-0"
          :class="{ 'bg-background': item.isUnread }"
        >
          <div class="flex items-start justify-between gap-2">
            <p class="text-sm font-medium">{{ item.title }}</p>
            <Badge :variant="LEVEL_VARIANTS[item.level] ?? 'neutral'">{{ item.level }}</Badge>
          </div>
          <p class="mt-1 text-sm text-text-muted">{{ item.body }}</p>
          <p class="mt-1 text-xs text-text-muted tabular-nums">
            {{ formatDate(item.createdAtUtc, { dateStyle: "short", timeStyle: "short" }) }}
          </p>
        </li>
      </ul>
    </div>
  </div>
</template>
