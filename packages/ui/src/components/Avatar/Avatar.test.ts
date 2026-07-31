import { mount } from "@vue/test-utils";
import { describe, expect, it } from "vitest";
import Avatar from "./Avatar.vue";

describe("Avatar", () => {
  it("derives initials from the first and last name", () => {
    const wrapper = mount(Avatar, { props: { name: "Ada Lovelace" } });

    expect(wrapper.text()).toBe("AL");
  });

  it("handles a single-word name", () => {
    const wrapper = mount(Avatar, { props: { name: "Ada" } });

    expect(wrapper.text()).toBe("A");
  });

  it("ignores middle names and extra whitespace", () => {
    const wrapper = mount(Avatar, { props: { name: "  Ada  Byron   King  " } });

    expect(wrapper.text()).toBe("AK");
  });

  it("shows the image when a source is provided", () => {
    const wrapper = mount(Avatar, { props: { name: "Ada", src: "https://example.test/a.png" } });

    expect(wrapper.find("img").exists()).toBe(true);
  });

  it("falls back to initials when the image fails to load", async () => {
    const wrapper = mount(Avatar, { props: { name: "Ada Lovelace", src: "broken.png" } });

    await wrapper.get("img").trigger("error");

    expect(wrapper.find("img").exists()).toBe(false);
    expect(wrapper.text()).toBe("AL");
  });

  it("retries when the source changes, instead of staying on the fallback", async () => {
    const wrapper = mount(Avatar, { props: { name: "Ada", src: "broken.png" } });
    await wrapper.get("img").trigger("error");

    await wrapper.setProps({ src: "working.png" });

    expect(wrapper.find("img").exists()).toBe(true);
  });

  it("exposes the name as the accessible name", () => {
    const wrapper = mount(Avatar, { props: { name: "Ada Lovelace" } });

    expect(wrapper.attributes("role")).toBe("img");
    expect(wrapper.attributes("aria-label")).toBe("Ada Lovelace");
  });

  it("hides itself when decorative, to avoid announcing the name twice", () => {
    const wrapper = mount(Avatar, { props: { name: "Ada Lovelace", decorative: true } });

    expect(wrapper.attributes("aria-hidden")).toBe("true");
    expect(wrapper.attributes("aria-label")).toBeUndefined();
  });
});
