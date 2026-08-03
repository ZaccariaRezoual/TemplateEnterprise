import { mount } from "@vue/test-utils";
import { describe, expect, it } from "vitest";
import Input from "./Input.vue";

describe("Input", () => {
  it("binds the label to the input so clicking it focuses the field", () => {
    const wrapper = mount(Input, { props: { label: "Email" } });

    const id = wrapper.get("input").attributes("id");
    expect(id).toBeTruthy();
    expect(wrapper.get("label").attributes("for")).toBe(id);
  });

  it("keeps the label available to screen readers when visually hidden", () => {
    const wrapper = mount(Input, { props: { label: "Search", labelHidden: true } });

    expect(wrapper.get("label").classes()).toContain("sr-only");
    expect(wrapper.get("label").text()).toContain("Search");
  });

  it("updates the model on input", async () => {
    const wrapper = mount(Input, { props: { label: "Email", modelValue: "" } });

    await wrapper.get("input").setValue("user@example.com");

    expect(wrapper.emitted("update:modelValue")?.at(-1)).toEqual(["user@example.com"]);
  });

  it("describes the field with the hint when valid", () => {
    const wrapper = mount(Input, { props: { label: "Password", hint: "At least 12 characters" } });

    const describedBy = wrapper.get("input").attributes("aria-describedby");
    expect(describedBy).toBeTruthy();
    expect(wrapper.get(`#${describedBy}`).text()).toBe("At least 12 characters");
  });

  it("marks the field invalid and announces the error", () => {
    const wrapper = mount(Input, { props: { label: "Email", error: "Enter a valid email." } });

    const input = wrapper.get("input");
    expect(input.attributes("aria-invalid")).toBe("true");

    const error = wrapper.get("[role='alert']");
    expect(error.text()).toBe("Enter a valid email.");
    expect(input.attributes("aria-describedby")).toBe(error.attributes("id"));
  });

  it("replaces the hint with the error rather than describing both", () => {
    const wrapper = mount(Input, {
      props: { label: "Email", hint: "Work address", error: "Enter a valid email." },
    });

    expect(wrapper.text()).not.toContain("Work address");
    expect(wrapper.findAll("[role='alert']")).toHaveLength(1);
  });

  it("exposes the required state to assistive technology, not only visually", () => {
    const wrapper = mount(Input, { props: { label: "Email", required: true } });

    expect(wrapper.get("input").attributes("aria-required")).toBe("true");
  });

  it("keeps the model a STRING even on a number field", async () => {
    // Vue's own v-model coerces the value to a NUMBER when the element is
    // type="number". The component would then break its declared contract,
    // and the mismatch surfaces only at runtime — in whichever page first
    // calls .trim() on what it was promised is a string.
    const wrapper = mount(Input, { props: { label: "Duration", type: "number" } });

    await wrapper.get("input").setValue("60");

    const emitted = wrapper.emitted("update:modelValue")?.at(-1)?.[0];
    expect(emitted).toBe("60");
    expect(typeof emitted).toBe("string");
  });

  it("generates unique ids so two fields on one page never collide", () => {
    // Both fields must live in the SAME app: Vue's useId counter is per-app,
    // so mounting twice in isolation would trivially produce the same id and
    // prove nothing about the real scenario.
    const wrapper = mount(
      {
        components: { Input },
        template: `<form><Input label="First" /><Input label="Second" /></form>`,
      },
      { global: { components: { Input } } },
    );

    const [first, second] = wrapper.findAll("input");
    expect(first?.attributes("id")).toBeTruthy();
    expect(first?.attributes("id")).not.toBe(second?.attributes("id"));
  });
});
