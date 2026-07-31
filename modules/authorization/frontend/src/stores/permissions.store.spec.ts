import { mount } from "@vue/test-utils";
import { createPinia, setActivePinia } from "pinia";
import { beforeEach, describe, expect, it, vi } from "vitest";
import { vCan } from "../directives/vCan";
import { provideAuthorizationApi, usePermissionsStore } from "./permissions.store";

/** Minimal SDK client stub returning a fixed authorization payload. */
function mockApi(roles: string[], permissions: string[]) {
  return {
    GET: vi.fn(async () => ({
      data: { roles, permissions },
      response: new Response(null, { status: 200 }),
    })),
  } as never;
}

function failingApi() {
  return {
    GET: vi.fn(async () => ({
      error: { title: "Unauthorized" },
      response: new Response(null, { status: 401 }),
    })),
  } as never;
}

describe("usePermissionsStore", () => {
  beforeEach(() => {
    setActivePinia(createPinia());
  });

  it("loads roles and permissions from the API", async () => {
    provideAuthorizationApi(mockApi(["Administrator"], ["users.read", "users.write"]));
    const store = usePermissionsStore();

    await store.load();

    expect(store.roles).toEqual(["Administrator"]);
    expect(store.can("users.read")).toBe(true);
    expect(store.isLoaded).toBe(true);
  });

  it("reports missing permissions as denied", async () => {
    provideAuthorizationApi(mockApi(["User"], []));
    const store = usePermissionsStore();

    await store.load();

    expect(store.can("users.read")).toBe(false);
    expect(store.canAny(["users.read", "users.write"])).toBe(false);
  });

  it("canAny passes when at least one permission is held", async () => {
    provideAuthorizationApi(mockApi(["Editor"], ["users.read"]));
    const store = usePermissionsStore();

    await store.load();

    expect(store.canAny(["users.write", "users.read"])).toBe(true);
  });

  it("degrades to no permissions when the session is not valid", async () => {
    provideAuthorizationApi(failingApi());
    const store = usePermissionsStore();

    await store.load();

    expect(store.permissions).toEqual([]);
    expect(store.isLoaded).toBe(false);
  });

  it("clear drops the set so the next user never inherits it", async () => {
    provideAuthorizationApi(mockApi(["Administrator"], ["users.read"]));
    const store = usePermissionsStore();
    await store.load();

    store.clear();

    expect(store.can("users.read")).toBe(false);
    expect(store.roles).toEqual([]);
  });
});

describe("v-can directive", () => {
  beforeEach(() => {
    setActivePinia(createPinia());
  });

  function render() {
    return mount(
      {
        template: `<div><button v-can="'users.write'">Edit</button></div>`,
      },
      { global: { directives: { can: vCan } } },
    );
  }

  it("removes the element from the DOM when the permission is missing", async () => {
    provideAuthorizationApi(mockApi(["User"], []));
    await usePermissionsStore().load();

    const wrapper = render();

    // Removed, not hidden: it must not be focusable or announced.
    expect(wrapper.find("button").exists()).toBe(false);
  });

  it("keeps the element when the permission is held", async () => {
    provideAuthorizationApi(mockApi(["Administrator"], ["users.write"]));
    await usePermissionsStore().load();

    const wrapper = render();

    expect(wrapper.find("button").exists()).toBe(true);
  });
});
