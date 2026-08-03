import { mount } from "@vue/test-utils";
import { describe, expect, it } from "vitest";
import Checkbox from "./Checkbox.vue";

describe("Checkbox", () => {
  it("binds the label to the input", () => {
    const wrapper = mount(Checkbox, { props: { label: "Published" } });

    const id = wrapper.get("input").attributes("id");
    expect(wrapper.get("label").attributes("for")).toBe(id);
  });

  it("uses a native checkbox so keyboard and forms work without help", () => {
    const wrapper = mount(Checkbox, { props: { label: "Published" } });

    expect(wrapper.get("input").attributes("type")).toBe("checkbox");
  });

  it("exposes the state through v-model", async () => {
    const wrapper = mount(Checkbox, { props: { label: "Published", modelValue: false } });

    await wrapper.get("input").setValue(true);

    expect(wrapper.emitted("update:modelValue")?.at(-1)).toEqual([true]);
  });

  it("announces the error and marks the control invalid", () => {
    const wrapper = mount(Checkbox, {
      props: { label: "Published", error: "Pick one option." },
    });

    const error = wrapper.get("[role='alert']");
    expect(error.text()).toBe("Pick one option.");
    expect(wrapper.get("input").attributes("aria-invalid")).toBe("true");
    expect(wrapper.get("input").attributes("aria-describedby")).toBe(error.attributes("id"));
  });

  it("describes the control with the hint until an error replaces it", async () => {
    const wrapper = mount(Checkbox, {
      props: { label: "Published", hint: "Visitors will see it." },
    });
    const hintId = wrapper.get("input").attributes("aria-describedby");
    expect(wrapper.get(`#${hintId}`).text()).toBe("Visitors will see it.");

    await wrapper.setProps({ error: "Not allowed." });

    // Only one description at a time: two would be read one after the other,
    // and the hint is the less urgent of the two.
    expect(wrapper.findAll("p")).toHaveLength(1);
  });

  it("makes the whole label row the hit area, not just the box", () => {
    const wrapper = mount(Checkbox, { props: { label: "Published" } });

    // The input lives INSIDE the label: on a phone the 16px box alone is far
    // below the 44px target the tokens require.
    expect(wrapper.get("label").find("input").exists()).toBe(true);
  });
});
