import { mount } from "@vue/test-utils";
import { describe, expect, it } from "vitest";
import PageSection from "./PageSection.vue";

describe("PageSection", () => {
  it("renders the heading at the requested level, not at a fixed one", () => {
    const wrapper = mount(PageSection, { props: { title: "Servizi", headingLevel: "h1" } });

    // The outline of a page is not a visual choice: a component that always
    // emitted h2 would quietly break it.
    expect(wrapper.find("h1").text()).toBe("Servizi");
    expect(wrapper.find("h2").exists()).toBe(false);
  });

  it("defaults to h2, the level a section under a page title needs", () => {
    const wrapper = mount(PageSection, { props: { title: "Servizi" } });

    expect(wrapper.find("h2").exists()).toBe(true);
  });

  it("takes its size from the type-role token, never from the heading level", () => {
    const wrapper = mount(PageSection, { props: { title: "Servizi", headingLevel: "h1" } });

    expect(wrapper.get("h1").classes()).toContain("text-display");
    expect(wrapper.attributes("class")).not.toMatch(/text-\dxl/);
  });

  it("keeps the intro within a readable measure", () => {
    const wrapper = mount(PageSection, { props: { intro: "Una frase." } });

    // A full-width paragraph on a laptop is the most common readability
    // mistake on a marketing page.
    expect(wrapper.get("p").classes()).toContain("max-w-prose");
  });

  it("renders a band of pure content when it has neither title nor intro", () => {
    const wrapper = mount(PageSection, { slots: { default: "<div data-testid='body' />" } });

    expect(wrapper.find("h1,h2,h3").exists()).toBe(false);
    expect(wrapper.find("p").exists()).toBe(false);
    expect(wrapper.find("[data-testid='body']").exists()).toBe(true);
  });

  it("owns the vertical rhythm through the band tokens", () => {
    const wrapper = mount(PageSection);

    // The public surface redefines what a band is worth; the same markup then
    // reads as a marketing band outside /admin and a compact one inside.
    expect(wrapper.classes()).toContain("py-band");
    expect(wrapper.classes()).toContain("sm:py-band-lg");
  });
});
