import { VueQueryPlugin } from "@tanstack/vue-query";
import { flushPromises, mount } from "@vue/test-utils";
import { describe, expect, it, vi } from "vitest";
import { provideDashboardApi } from "../api/dashboard.api";
import DashboardGrid from "./DashboardGrid.vue";

/** SDK stub answering the widgets endpoint. */
function mockApi(widgets: unknown[]) {
  return {
    GET: vi.fn(async () => ({
      data: widgets,
      response: new Response(null, { status: 200 }),
    })),
  } as never;
}

const statWidget = {
  id: "users.total",
  title: "Users",
  kind: "Stat",
  value: "42",
  caption: "38 active",
  link: "/users",
};

const listWidget = {
  id: "audit.recent",
  title: "Recent activity",
  kind: "List",
  items: [{ label: "user.created", detail: "2 minutes ago" }],
};

async function renderGrid(widgets: unknown[]) {
  provideDashboardApi(mockApi(widgets));

  const wrapper = mount(DashboardGrid, {
    global: { plugins: [VueQueryPlugin], stubs: { RouterLink: { template: "<a><slot/></a>" } } },
  });
  await flushPromises();
  return wrapper;
}

describe("DashboardGrid", () => {
  it("renders a stat widget from what the server returned", async () => {
    const wrapper = await renderGrid([statWidget]);

    expect(wrapper.text()).toContain("Users");
    expect(wrapper.text()).toContain("42");
    expect(wrapper.text()).toContain("38 active");
  });

  it("renders a list widget as entries, not as a figure", async () => {
    const wrapper = await renderGrid([listWidget]);

    expect(wrapper.findAll("li")).toHaveLength(1);
    expect(wrapper.text()).toContain("user.created");
    expect(wrapper.text()).toContain("2 minutes ago");
  });

  it("renders whatever kinds arrive together, in server order", async () => {
    const wrapper = await renderGrid([statWidget, listWidget]);

    // The client holds no list of known widgets: a module contributing a tile
    // must show up here without this component changing.
    expect(wrapper.text()).toContain("42");
    expect(wrapper.text()).toContain("Recent activity");
  });

  it("says the dashboard is empty rather than showing nothing", async () => {
    const wrapper = await renderGrid([]);

    expect(wrapper.text()).toContain("No widgets yet");
  });

  it("marks the region busy while loading", async () => {
    provideDashboardApi({ GET: vi.fn(() => new Promise(() => {})) } as never);

    const wrapper = mount(DashboardGrid, { global: { plugins: [VueQueryPlugin] } });

    expect(wrapper.get("[role='region']").attributes("aria-busy")).toBe("true");
  });
});
