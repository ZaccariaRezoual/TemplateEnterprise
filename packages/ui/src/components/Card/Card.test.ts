import { mount } from "@vue/test-utils";
import { describe, expect, it } from "vitest";
import Card from "./Card.vue";

describe("Card", () => {
  it("renders its content", () => {
    const wrapper = mount(Card, { slots: { default: "Body content" } });

    expect(wrapper.text()).toContain("Body content");
  });

  it("labels the region with its title so screen readers can navigate by landmark", () => {
    const wrapper = mount(Card, { props: { title: "Revenue" }, slots: { default: "x" } });

    const labelledBy = wrapper.get("section").attributes("aria-labelledby");
    expect(wrapper.get(`#${labelledBy}`).text()).toBe("Revenue");
  });

  it("respects the requested heading level to keep the document outline valid", () => {
    const wrapper = mount(Card, {
      props: { title: "Revenue", headingLevel: "h2" },
      slots: { default: "x" },
    });

    expect(wrapper.find("h2").exists()).toBe(true);
    expect(wrapper.find("h3").exists()).toBe(false);
  });

  it("omits the header entirely when there is nothing to put in it", () => {
    const wrapper = mount(Card, { slots: { default: "x" } });

    expect(wrapper.find("header").exists()).toBe(false);
    expect(wrapper.get("section").attributes("aria-labelledby")).toBeUndefined();
  });

  it("renders the footer only when provided", () => {
    const withFooter = mount(Card, { slots: { default: "x", footer: "Actions" } });
    const without = mount(Card, { slots: { default: "x" } });

    expect(withFooter.find("footer").text()).toBe("Actions");
    expect(without.find("footer").exists()).toBe(false);
  });

  it("drops body padding when flush, for content that manages its own", () => {
    const wrapper = mount(Card, { props: { flush: true }, slots: { default: "x" } });

    expect(wrapper.html()).not.toContain("pb-5");
  });
});
