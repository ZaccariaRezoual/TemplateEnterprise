import { VueQueryPlugin } from "@tanstack/vue-query";
import { provideAuthorizationApi, usePermissionsStore } from "@enterprise/module-authorization";
import { flushPromises, mount } from "@vue/test-utils";
import { createPinia, setActivePinia } from "pinia";
import { beforeEach, describe, expect, it, vi } from "vitest";
import { defineComponent, h } from "vue";
import { provideServicesApi } from "../api/services.api";
import ServicesAdminPage from "./ServicesAdminPage.vue";

/**
 * A RouterLink that actually renders its slot, scoped one included.
 *
 * The default `stubs: { RouterLink: true }` drops the slot content, so the
 * "Nuovo servizio" button — which is a `Button` inside a `custom` RouterLink
 * — would simply not exist and the assertions about it would pass vacuously.
 */
const RouterLink = defineComponent({
  props: { to: { type: [String, Object], required: true }, custom: Boolean },
  setup(props, { slots }) {
    return () => {
      const href = typeof props.to === "string" ? props.to : "#";
      return props.custom
        ? slots["default"]?.({ href, navigate: () => {} })
        : h("a", { href }, slots["default"]?.());
    };
  },
});

/** SDK stub answering both the permissions and the catalogue endpoints. */
function mockApi(permissions: string[], services: unknown[] = []) {
  return {
    GET: vi.fn(async (path: string) => {
      if (path === "/api/authorization/me") {
        return {
          data: { roles: ["Tester"], permissions },
          response: new Response(null, { status: 200 }),
        };
      }
      return { data: services, response: new Response(null, { status: 200 }) };
    }),
  } as never;
}

function service(overrides: Record<string, unknown> = {}) {
  return {
    id: "00000000-0000-4000-8000-000000000001",
    title: "Consulenza strategica",
    slug: "consulenza-strategica",
    shortDescription: "Un'ora per mettere a fuoco il problema.",
    description: "",
    durationMinutes: 60,
    price: null,
    currency: null,
    isPublished: true,
    isBookable: false,
    sortOrder: 0,
    isArchived: false,
    images: [],
    createdAtUtc: "2026-01-02T00:00:00Z",
    updatedAtUtc: "2026-01-02T00:00:00Z",
    ...overrides,
  };
}

async function renderPage(permissions: string[], services: unknown[] = []) {
  const api = mockApi(permissions, services);
  provideAuthorizationApi(api);
  provideServicesApi(api, "");
  await usePermissionsStore().load();

  const wrapper = mount(ServicesAdminPage, {
    global: { plugins: [VueQueryPlugin], stubs: { RouterLink } },
  });
  // Twice: the first tick resolves the query, the second lets the component
  // re-render with its data.
  await flushPromises();
  await flushPromises();
  return wrapper;
}

describe("ServicesAdminPage", () => {
  beforeEach(() => {
    setActivePinia(createPinia());
  });

  it("explains the missing permission instead of showing an empty table", async () => {
    const wrapper = await renderPage([]);

    // An empty table would read as "there are no services", which is a lie.
    expect(wrapper.get("[role='alert']").text()).toContain("permesso");
    expect(wrapper.find("table").exists()).toBe(false);
  });

  it("lists the catalogue when the read permission is held", async () => {
    const wrapper = await renderPage(["services.read"], [service()]);

    expect(wrapper.find("table").exists()).toBe(true);
    expect(wrapper.text()).toContain("Consulenza strategica");
    expect(wrapper.text()).toContain("/consulenza-strategica");
  });

  it("tells drafts apart from published entries", async () => {
    const wrapper = await renderPage(["services.read"], [service({ isPublished: false })]);

    expect(wrapper.text()).toContain("Bozza");
    expect(wrapper.text()).not.toContain("Pubblicato");
  });

  it("hides archived entries until they are asked for", async () => {
    const wrapper = await renderPage(["services.read"], [service({ isArchived: true })]);

    // The catalogue is not empty, so the empty state must not claim it is.
    expect(wrapper.text()).toContain("Nessun servizio corrisponde alla ricerca");
    expect(wrapper.text()).not.toContain("Consulenza strategica");
  });

  it("says 'no services yet' rather than 'nothing matches' on an empty catalogue", async () => {
    const wrapper = await renderPage(["services.read"], []);

    // Two different facts, and telling them apart is the difference between
    // "create one" and "clear the search".
    expect(wrapper.text()).toContain("Nessun servizio: creane uno");
  });

  it("does not offer to create or archive without the write permissions", async () => {
    const wrapper = await renderPage(["services.read"], [service()]);

    expect(wrapper.text()).not.toContain("Nuovo servizio");
    expect(wrapper.text()).not.toContain("Archivia");
    expect(wrapper.text()).not.toContain("Modifica");
  });

  it("offers archiving only to who may delete", async () => {
    const wrapper = await renderPage(
      ["services.read", "services.write", "services.delete"],
      [service()],
    );

    expect(wrapper.text()).toContain("Archivia");
    expect(wrapper.text()).toContain("Modifica");
  });

  it("asks for confirmation before archiving", async () => {
    const wrapper = await renderPage(["services.read", "services.delete"], [service()]);

    await wrapper
      .findAll("button")
      .find((button) => button.text() === "Archivia")
      ?.trigger("click");

    // Archiving cannot be undone — the way back is a new service — so it gets
    // a second step.
    expect(wrapper.text()).toContain("Confermi?");
  });
});
