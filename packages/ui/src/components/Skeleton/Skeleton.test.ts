import { mount } from "@vue/test-utils";
import { describe, expect, it } from "vitest";
import Skeleton from "./Skeleton.vue";

describe("Skeleton", () => {
  it("is hidden from assistive technology", () => {
    const wrapper = mount(Skeleton);

    expect(wrapper.attributes("aria-hidden")).toBe("true");
  });

  it("defaults to a single text line", () => {
    const wrapper = mount(Skeleton);

    expect(wrapper.classes()).toContain("h-[1em]");
  });

  it("applies the given dimensions", () => {
    const wrapper = mount(Skeleton, { props: { shape: "block", width: "60%", height: "4rem" } });

    expect(wrapper.attributes("style")).toContain("width: 60%");
    expect(wrapper.attributes("style")).toContain("height: 4rem");
  });

  it("keeps a circle square so it cannot render as an ellipse", () => {
    const wrapper = mount(Skeleton, { props: { shape: "circle", width: "2.5rem" } });

    expect(wrapper.classes()).toContain("aspect-square");
  });

  it("stops pulsing when the user asked for reduced motion", () => {
    const wrapper = mount(Skeleton);

    expect(wrapper.classes()).toContain("motion-reduce:animate-none");
  });
});
