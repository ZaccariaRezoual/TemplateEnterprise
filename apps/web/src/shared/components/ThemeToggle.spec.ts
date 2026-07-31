import { mount } from "@vue/test-utils";
import { beforeEach, describe, expect, it } from "vitest";
import ThemeToggle from "@/shared/components/ThemeToggle.vue";

describe("ThemeToggle", () => {
  beforeEach(() => {
    globalThis.localStorage.clear();
  });

  it("exposes the three options as an accessible radio group", () => {
    const wrapper = mount(ThemeToggle);

    expect(wrapper.get("[role='radiogroup']").attributes("aria-label")).toBe("Color theme");
    expect(wrapper.findAll("[role='radio']")).toHaveLength(3);
  });

  it("marks the selected option and applies it to the document", async () => {
    const wrapper = mount(ThemeToggle);

    await wrapper.findAll("[role='radio']")[1]?.trigger("click");

    const [light, dark] = wrapper.findAll("[role='radio']");
    expect(dark?.attributes("aria-checked")).toBe("true");
    expect(light?.attributes("aria-checked")).toBe("false");
  });

  it("persists the choice so it survives a reload", async () => {
    const wrapper = mount(ThemeToggle);

    await wrapper.findAll("[role='radio']")[0]?.trigger("click");

    expect(globalThis.localStorage.getItem("ef:theme")).toBe('"light"');
  });
});
