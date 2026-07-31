import { VueQueryPlugin } from "@tanstack/vue-query";
import { provideAuthorizationApi, usePermissionsStore } from "@enterprise/module-authorization";
import { flushPromises, mount } from "@vue/test-utils";
import { createPinia, setActivePinia } from "pinia";
import { beforeEach, describe, expect, it, vi } from "vitest";
import { provideUsersApi } from "../api/users.api";
import UsersPage from "./UsersPage.vue";

/** SDK stub answering both the permissions and the users endpoints. */
function mockApi(permissions: string[], items: unknown[] = []) {
  return {
    GET: vi.fn(async (path: string) => {
      if (path === "/api/authorization/me") {
        return {
          data: { roles: ["Tester"], permissions },
          response: new Response(null, { status: 200 }),
        };
      }
      return {
        data: { items, page: 1, pageSize: 25, totalCount: items.length },
        response: new Response(null, { status: 200 }),
      };
    }),
  } as never;
}

const sampleUser = {
  id: "00000000-0000-4000-8000-000000000001",
  email: "ada@example.com",
  displayName: "Ada Lovelace",
  jobTitle: null,
  isActive: true,
  createdAtUtc: "2026-01-02T00:00:00Z",
};

async function renderPage(permissions: string[], items: unknown[] = []) {
  const api = mockApi(permissions, items);
  provideAuthorizationApi(api);
  provideUsersApi(api);
  await usePermissionsStore().load();

  const wrapper = mount(UsersPage, {
    global: { plugins: [VueQueryPlugin], stubs: { RouterLink: true } },
  });
  await flushPromises();
  return wrapper;
}

describe("UsersPage", () => {
  beforeEach(() => {
    setActivePinia(createPinia());
  });

  it("explains the missing permission instead of showing an empty table", async () => {
    const wrapper = await renderPage([]);

    // An empty table would read as "there are no users", which is a lie.
    expect(wrapper.get("[role='alert']").text()).toContain("do not have permission");
    expect(wrapper.find("table").exists()).toBe(false);
  });

  it("lists users when the read permission is held", async () => {
    const wrapper = await renderPage(["users.read"], [sampleUser]);

    expect(wrapper.find("table").exists()).toBe(true);
    expect(wrapper.text()).toContain("Ada Lovelace");
    expect(wrapper.text()).toContain("ada@example.com");
    expect(wrapper.get("[data-testid='users-total']").text()).toContain("1");
  });

  it("shows an explicit empty state when the search matches nothing", async () => {
    const wrapper = await renderPage(["users.read"], []);

    expect(wrapper.text()).toContain("No users match this search.");
  });

  it("marks inactive accounts", async () => {
    const wrapper = await renderPage(["users.read"], [{ ...sampleUser, isActive: false }]);

    expect(wrapper.text()).toContain("Inactive");
  });
});
