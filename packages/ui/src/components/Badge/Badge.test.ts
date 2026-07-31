import { mount } from "@vue/test-utils";
import { describe, expect, it } from "vitest";
import Badge from "./Badge.vue";

describe("Badge", () => {
  it("renders its label", () => {
    const wrapper = mount(Badge, { slots: { default: "Active" } });

    expect(wrapper.text()).toBe("Active");
  });

  it("defaults to the neutral variant", () => {
    const wrapper = mount(Badge, { slots: { default: "Draft" } });

    expect(wrapper.classes()).toContain("text-text-muted");
  });

  it.each([
    ["success", "text-success"],
    ["warning", "text-warning"],
    ["danger", "text-danger"],
    ["info", "text-primary"],
  ] as const)("styles the %s variant from semantic tokens", (variant, expectedClass) => {
    const wrapper = mount(Badge, { props: { variant }, slots: { default: "Status" } });

    expect(wrapper.classes()).toContain(expectedClass);
  });

  it("adds a screen-reader label so status is not conveyed by color alone", () => {
    const wrapper = mount(Badge, {
      props: { variant: "success", srLabel: "Status: active" },
      slots: { default: "●" },
    });

    const hidden = wrapper.get(".sr-only");
    expect(hidden.text()).toBe("Status: active");
  });
});
