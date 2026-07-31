import { mount } from "@vue/test-utils";
import { createPinia, setActivePinia } from "pinia";
import { beforeEach, describe, expect, it, vi } from "vitest";
import NotificationBell from "../components/NotificationBell.vue";
import { provideNotificationsApi, useNotificationsStore } from "./notifications.store";

function notification(id: string, isUnread = true) {
  return {
    id,
    title: `Title ${id}`,
    body: "Body",
    level: "Info",
    link: null,
    isUnread,
    createdAtUtc: "2026-07-31T10:00:00Z",
  };
}

function mockApi(items: unknown[]) {
  return {
    GET: vi.fn(async () => ({ data: items, response: new Response(null, { status: 200 }) })),
    POST: vi.fn(async () => ({ data: null, response: new Response(null, { status: 204 }) })),
  } as never;
}

describe("useNotificationsStore", () => {
  beforeEach(() => {
    setActivePinia(createPinia());
  });

  it("counts only unread notifications", async () => {
    provideNotificationsApi(mockApi([notification("1"), notification("2", false)]));
    const store = useNotificationsStore();

    await store.load();

    expect(store.items).toHaveLength(2);
    expect(store.unreadCount).toBe(1);
  });

  it("keeps the page usable when loading fails", async () => {
    provideNotificationsApi({
      GET: vi.fn(async () => ({
        error: {},
        response: new Response(null, { status: 500 }),
      })),
    } as never);
    const store = useNotificationsStore();

    await store.load();

    expect(store.items).toEqual([]);
    expect(store.unreadCount).toBe(0);
  });

  it("markAsRead updates the badge immediately", async () => {
    provideNotificationsApi(mockApi([notification("1"), notification("2")]));
    const store = useNotificationsStore();
    await store.load();

    await store.markAsRead("1");

    expect(store.unreadCount).toBe(1);
  });

  it("markAsRead without an id clears every unread", async () => {
    provideNotificationsApi(mockApi([notification("1"), notification("2")]));
    const store = useNotificationsStore();
    await store.load();

    await store.markAsRead();

    expect(store.unreadCount).toBe(0);
  });

  it("receive ignores a notification already present", async () => {
    provideNotificationsApi(mockApi([notification("1")]));
    const store = useNotificationsStore();
    await store.load();

    // A realtime push can race with a refetch (Fase 6).
    store.receive(notification("1"));

    expect(store.items).toHaveLength(1);
  });
});

describe("NotificationBell", () => {
  beforeEach(() => {
    setActivePinia(createPinia());
  });

  it("puts the unread count in the accessible name, not just in a badge", async () => {
    provideNotificationsApi(mockApi([notification("1")]));
    const wrapper = mount(NotificationBell);
    await useNotificationsStore().load();
    await wrapper.vm.$nextTick();

    // A bare number is meaningless to a screen reader.
    expect(wrapper.get("[data-testid='notification-bell']").attributes("aria-label")).toBe(
      "Notifications, 1 unread",
    );
  });

  it("shows an explicit empty state", async () => {
    provideNotificationsApi(mockApi([]));
    const wrapper = mount(NotificationBell);

    await wrapper.get("[data-testid='notification-bell']").trigger("click");
    await wrapper.vm.$nextTick();

    expect(wrapper.get("[data-testid='notifications-empty']").text()).toContain("no notifications");
  });
});
