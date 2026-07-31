import { mount } from "@vue/test-utils";
import { describe, expect, it } from "vitest";
import Button from "./Button.vue";

describe("Button", () => {
  it("renders its label and defaults to type=button so it never submits a form by accident", () => {
    const wrapper = mount(Button, { slots: { default: "Save" } });

    expect(wrapper.text()).toBe("Save");
    expect(wrapper.attributes("type")).toBe("button");
  });

  it("emits click when interactive", async () => {
    const wrapper = mount(Button, { slots: { default: "Save" } });

    await wrapper.trigger("click");

    expect(wrapper.emitted("click")).toHaveLength(1);
  });

  it("does not emit click when disabled", async () => {
    const wrapper = mount(Button, { props: { disabled: true }, slots: { default: "Save" } });

    await wrapper.trigger("click");

    expect(wrapper.emitted("click")).toBeUndefined();
  });

  it("blocks interaction and reports busy state while loading", async () => {
    const wrapper = mount(Button, { props: { loading: true }, slots: { default: "Save" } });

    await wrapper.trigger("click");

    expect(wrapper.emitted("click")).toBeUndefined();
    expect(wrapper.attributes("aria-busy")).toBe("true");
    expect(wrapper.attributes("disabled")).toBeDefined();
    expect(wrapper.find("svg").exists()).toBe(true);
  });

  it("hides the icon slot while loading so the spinner is unambiguous", () => {
    const wrapper = mount(Button, {
      props: { loading: true },
      slots: { default: "Save", icon: "<span data-testid='icon' />" },
    });

    expect(wrapper.find("[data-testid='icon']").exists()).toBe(false);
  });

  it("applies variant styling from semantic tokens, never literal colors", () => {
    const wrapper = mount(Button, { props: { variant: "danger" }, slots: { default: "Delete" } });

    expect(wrapper.classes()).toContain("bg-danger");
    expect(wrapper.attributes("class")).not.toMatch(/#[0-9a-f]{3,6}/i);
  });

  it("lets a consumer override a conflicting utility instead of stacking both", () => {
    const wrapper = mount(Button, {
      attrs: { class: "bg-surface" },
      slots: { default: "Save" },
    });

    expect(wrapper.classes()).toContain("bg-surface");
    expect(wrapper.classes()).not.toContain("bg-primary");
  });
});
