import { mount } from "@vue/test-utils";
import { describe, expect, it } from "vitest";
import Textarea from "./Textarea.vue";

describe("Textarea", () => {
  it("binds the label to the field", () => {
    const wrapper = mount(Textarea, { props: { label: "Message" } });

    const id = wrapper.get("textarea").attributes("id");
    expect(wrapper.get("label").attributes("for")).toBe(id);
  });

  it("announces the error and marks the field invalid", () => {
    const wrapper = mount(Textarea, { props: { label: "Message", error: "Write something." } });

    const error = wrapper.get("[role='alert']");
    expect(error.text()).toBe("Write something.");
    expect(wrapper.get("textarea").attributes("aria-invalid")).toBe("true");
    expect(wrapper.get("textarea").attributes("aria-describedby")).toBe(error.attributes("id"));
  });

  it("describes the field with the hint until an error replaces it", async () => {
    const wrapper = mount(Textarea, { props: { label: "Message", hint: "Max 500 characters." } });
    const hintId = wrapper.get("textarea").attributes("aria-describedby");
    expect(wrapper.get(`#${hintId}`).text()).toBe("Max 500 characters.");

    await wrapper.setProps({ error: "Too long." });

    // Only one description at a time: two would be read one after the other,
    // and the hint is the less urgent of the two.
    expect(wrapper.findAll("p")).toHaveLength(1);
  });

  it("counts characters only when a limit exists", async () => {
    const wrapper = mount(Textarea, { props: { label: "Message" } });
    expect(wrapper.text()).not.toContain("/");

    await wrapper.setProps({ maxlength: 100 });
    await wrapper.get("textarea").setValue("hello");

    expect(wrapper.text()).toContain("5/100");
  });

  it("marks a required field for assistive technology, not just visually", () => {
    const wrapper = mount(Textarea, { props: { label: "Message", required: true } });

    expect(wrapper.get("textarea").attributes("aria-required")).toBe("true");
  });
});
