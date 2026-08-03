import { VueQueryPlugin } from "@tanstack/vue-query";
import { flushPromises, mount } from "@vue/test-utils";
import { describe, expect, it, vi } from "vitest";
import { defineComponent, h } from "vue";
import { provideServicesApi } from "../api/services.api";
import ServicesShowcasePage from "./ServicesShowcasePage.vue";

/**
 * A RouterLink that actually renders its slot.
 *
 * The default `stubs: { RouterLink: true }` drops the slot content, which
 * would make every assertion about what is INSIDE a card pass vacuously.
 * It also honours `custom`, so the scoped-slot form used for button-shaped
 * links renders too.
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

function publicService(overrides: Record<string, unknown> = {}) {
  return {
    id: "00000000-0000-4000-8000-000000000001",
    title: "Consulenza strategica",
    slug: "consulenza-strategica",
    shortDescription: "Un'ora per mettere a fuoco il problema.",
    description: "Testo lungo.",
    durationMinutes: 60,
    price: null,
    currency: null,
    isBookable: false,
    images: [],
    ...overrides,
  };
}

async function renderPage(services: unknown[], status = 200) {
  provideServicesApi(
    {
      GET: vi.fn(async () =>
        status === 200
          ? { data: services, response: new Response(null, { status }) }
          : { error: {}, response: new Response(null, { status }) },
      ),
    } as never,
    "",
  );

  const wrapper = mount(ServicesShowcasePage, {
    global: {
      plugins: [
        [
          VueQueryPlugin,
          // No retries in tests: the failure path is what is being asserted,
          // and the default backoff would keep the page in its loading state
          // for seconds before ever reaching it.
          { queryClientConfig: { defaultOptions: { queries: { retry: false } } } },
        ],
      ],
      stubs: { RouterLink },
    },
  });
  // Twice: the first tick resolves the query, the second lets the component
  // re-render with its data.
  await flushPromises();
  await flushPromises();
  return wrapper;
}

describe("ServicesShowcasePage", () => {
  it("shows the published services", async () => {
    const wrapper = await renderPage([publicService()]);

    expect(wrapper.text()).toContain("Consulenza strategica");
    expect(wrapper.text()).toContain("Un'ora per mettere a fuoco il problema.");
  });

  it("shows the duration only when it was published", async () => {
    const withDuration = await renderPage([publicService()]);
    expect(withDuration.text()).toContain("60 min");

    const without = await renderPage([publicService({ durationMinutes: null })]);
    expect(without.text()).not.toContain("min");
  });

  it("says so when there is nothing published yet", async () => {
    const wrapper = await renderPage([]);

    expect(wrapper.text()).toContain("Non ci sono ancora servizi pubblicati.");
  });

  it("keeps its heading when the catalogue cannot be loaded", async () => {
    const wrapper = await renderPage([], 500);

    // The heading is what the prerender waits for, so an API failure must not
    // leave the page without one.
    expect(wrapper.find("h1").exists()).toBe(true);
    expect(wrapper.get("[role='alert']").text()).toContain("Non siamo riusciti");
  });
});
