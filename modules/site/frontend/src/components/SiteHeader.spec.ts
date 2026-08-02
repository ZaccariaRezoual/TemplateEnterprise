import { flushPromises, mount, type VueWrapper } from "@vue/test-utils";
import { beforeEach, describe, expect, it } from "vitest";
import { createMemoryHistory, createRouter, type Router } from "vue-router";
import { defaultSiteContent, provideSiteContent } from "../content";
import { installSiteModule } from "../install";
import SiteHeader from "./SiteHeader.vue";

const blank = { template: "<div />" };

/**
 * Whether the disclosure panel is showing.
 *
 * Reads what `v-show` actually does — the inline `display` — instead of
 * `isVisible()`: without a stylesheet jsdom cannot resolve the utility
 * classes, so a visibility helper answers a question the test never asked.
 */
function menuIsOpen(wrapper: VueWrapper): boolean {
  return (wrapper.get("#site-menu").element as HTMLElement).style.display !== "none";
}

/**
 * A real router, not a stub: the header watches the route to close its menu,
 * and a stubbed `useRoute` would make that behaviour untestable — which is
 * precisely the behaviour worth testing.
 */
function createTestRouter(): Router {
  return createRouter({
    history: createMemoryHistory(),
    routes: [
      // Every path the header links to: an unmatched RouterLink aborts the
      // render half-way, which shows up as unrelated assertions failing.
      { path: "/", name: "site-home", component: blank },
      { path: "/about", name: "site-about", component: blank },
      { path: "/services", name: "site-services", component: blank },
      { path: "/contact", name: "site-contact", component: blank },
      { path: "/privacy", name: "site-privacy", component: blank },
      { path: "/login", name: "login", component: blank },
      { path: "/admin", name: "admin", component: blank },
    ],
  });
}

async function render() {
  const router = createTestRouter();
  await router.push("/");
  await router.isReady();

  const wrapper = mount(SiteHeader, { global: { plugins: [router] } });
  return { wrapper, router };
}

describe("SiteHeader", () => {
  beforeEach(() => {
    provideSiteContent(defaultSiteContent);
    installSiteModule();
  });

  it("shows the project's name from the content", async () => {
    provideSiteContent({ ...defaultSiteContent, name: "Acme" });

    const { wrapper } = await render();

    expect(wrapper.text()).toContain("Acme");
  });

  it("offers sign-in by default, without importing the auth module", async () => {
    const { wrapper } = await render();

    expect(wrapper.get("[data-testid='site-entry']").text()).toBe("Accedi");
  });

  it("uses the entry the host declared", async () => {
    installSiteModule({
      links: { entryPath: () => "/admin", entryLabel: () => "Area riservata" },
    });

    const { wrapper } = await render();

    expect(wrapper.get("[data-testid='site-entry']").text()).toBe("Area riservata");
  });

  it("keeps the mobile menu closed until it is tapped", async () => {
    const { wrapper } = await render();
    const toggle = wrapper.get("[data-testid='site-menu-toggle']");

    // A screen reader needs the state, not just the icon: an unlabelled
    // square that changes glyph tells it nothing.
    expect(toggle.attributes("aria-expanded")).toBe("false");
    expect(menuIsOpen(wrapper)).toBe(false);

    await toggle.trigger("click");

    expect(toggle.attributes("aria-expanded")).toBe("true");
    expect(menuIsOpen(wrapper)).toBe(true);
  });

  it("closes the menu after navigating", async () => {
    const { wrapper, router } = await render();
    await wrapper.get("[data-testid='site-menu-toggle']").trigger("click");

    await router.push("/about");
    await flushPromises();

    // A panel left open over the page the visitor just asked for is the
    // classic mobile-menu bug.
    expect(menuIsOpen(wrapper)).toBe(false);
  });
});
