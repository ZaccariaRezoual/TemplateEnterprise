import { mount } from "@vue/test-utils";
import { createPinia, setActivePinia } from "pinia";
import { beforeEach, describe, expect, it, vi } from "vitest";
import { useFeatureFlagsStore, vFeature } from "@/core/features/featureFlags";

vi.mock("@/core/api/apiClient", () => ({
  api: {},
  request: vi.fn(),
}));

const { request } = await import("@/core/api/apiClient");

function render() {
  return mount(
    { template: `<div><span v-feature="'beta'">Beta</span></div>` },
    { global: { directives: { feature: vFeature } } },
  );
}

describe("feature flags", () => {
  beforeEach(() => {
    setActivePinia(createPinia());
    vi.mocked(request).mockReset();
  });

  it("reports an unknown flag as off", () => {
    // A typo hides a feature rather than revealing an unfinished one.
    expect(useFeatureFlagsStore().isEnabled("never.declared")).toBe(false);
  });

  it("loads flags from the API", async () => {
    vi.mocked(request).mockResolvedValue({ beta: true, legacy: false });
    const store = useFeatureFlagsStore();

    await store.load();

    expect(store.isEnabled("beta")).toBe(true);
    expect(store.isEnabled("legacy")).toBe(false);
    expect(store.isLoaded).toBe(true);
  });

  it("treats every flag as off when loading fails", async () => {
    vi.mocked(request).mockRejectedValue(new Error("offline"));
    const store = useFeatureFlagsStore();

    await store.load();

    // Shipping the conservative UI beats rendering a half-built feature.
    expect(store.isEnabled("beta")).toBe(false);
    expect(store.isLoaded).toBe(false);
  });

  it("v-feature removes the element when the flag is off", async () => {
    vi.mocked(request).mockResolvedValue({ beta: false });
    await useFeatureFlagsStore().load();

    const wrapper = render();

    // Removed, not hidden: it must not be focusable or announced.
    expect(wrapper.find("span").exists()).toBe(false);
  });

  it("v-feature keeps the element when the flag is on", async () => {
    vi.mocked(request).mockResolvedValue({ beta: true });
    await useFeatureFlagsStore().load();

    const wrapper = render();

    expect(wrapper.find("span").exists()).toBe(true);
  });
});
