import { mount } from "@vue/test-utils";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import ToastHost from "./ToastHost.vue";
import { useToast } from "./useToast";

describe("useToast / ToastHost", () => {
  beforeEach(() => {
    vi.useFakeTimers();
    useToast().clear();
  });

  afterEach(() => {
    vi.useRealTimers();
  });

  it("renders a raised toast", async () => {
    const wrapper = mount(ToastHost);

    useToast().show({ title: "Saved" });
    await wrapper.vm.$nextTick();

    expect(wrapper.findAll("[data-testid='toast']")).toHaveLength(1);
    expect(wrapper.text()).toContain("Saved");
  });

  it("auto-dismisses after its duration", async () => {
    const wrapper = mount(ToastHost);
    useToast().show({ title: "Saved", duration: 1000 });
    await wrapper.vm.$nextTick();

    vi.advanceTimersByTime(1000);
    await wrapper.vm.$nextTick();

    expect(wrapper.findAll("[data-testid='toast']")).toHaveLength(0);
  });

  it("keeps errors until dismissed", async () => {
    const wrapper = mount(ToastHost);
    useToast().show({ title: "Upload failed", variant: "danger" });
    await wrapper.vm.$nextTick();

    vi.advanceTimersByTime(60_000);
    await wrapper.vm.$nextTick();

    // An error nobody read is reported as "nothing happened".
    expect(wrapper.findAll("[data-testid='toast']")).toHaveLength(1);
  });

  it("announces errors assertively and everything else politely", async () => {
    const wrapper = mount(ToastHost);

    useToast().show({ title: "Saved" });
    await wrapper.vm.$nextTick();
    expect(wrapper.get("[data-testid='toast']").attributes("role")).toBeUndefined();

    useToast().clear();
    useToast().show({ title: "Failed", variant: "danger" });
    await wrapper.vm.$nextTick();
    expect(wrapper.get("[data-testid='toast']").attributes("role")).toBe("alert");
  });

  it("caps the stack so it cannot cover the page", async () => {
    const wrapper = mount(ToastHost, { props: { max: 2 } });

    for (const title of ["one", "two", "three"]) {
      useToast().show({ title });
    }
    await wrapper.vm.$nextTick();

    expect(wrapper.findAll("[data-testid='toast']")).toHaveLength(2);
    // The newest survive.
    expect(wrapper.text()).toContain("three");
    expect(wrapper.text()).not.toContain("one");
  });

  it("dismisses through the button, not only the timeout", async () => {
    const wrapper = mount(ToastHost);
    useToast().show({ title: "Saved", duration: 0 });
    await wrapper.vm.$nextTick();

    await wrapper.get("button").trigger("click");

    expect(wrapper.findAll("[data-testid='toast']")).toHaveLength(0);
  });

  it("labels the dismiss button with the toast it closes", async () => {
    const wrapper = mount(ToastHost);
    useToast().show({ title: "Saved" });
    await wrapper.vm.$nextTick();

    // "Dismiss" repeated five times tells a screen-reader user nothing.
    expect(wrapper.get("button").attributes("aria-label")).toBe("Dismiss: Saved");
  });
});
