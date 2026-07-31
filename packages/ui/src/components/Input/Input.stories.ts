import type { Meta, StoryObj } from "@storybook/vue3-vite";
import Input from "./Input.vue";

/**
 * Storybook entry for {@link Input}.
 * Includes the states forms actually hit — hint, error, disabled — because
 * those are the ones that regress silently.
 */
const meta = {
  title: "Components/Input",
  component: Input,
  tags: ["autodocs"],
  args: { label: "Email address", placeholder: "you@example.com" },
  argTypes: {
    type: {
      control: "select",
      options: ["text", "email", "password", "search", "tel", "url", "number"],
    },
    disabled: { control: "boolean" },
    required: { control: "boolean" },
    labelHidden: { control: "boolean" },
  },
} satisfies Meta<typeof Input>;

export default meta;

type Story = StoryObj<typeof meta>;

/** Standard labelled field. */
export const Default: Story = {};

/** With helper text describing the expected value. */
export const WithHint: Story = { args: { hint: "We only use this to sign you in." } };

/** Invalid state: the message is announced and replaces the hint. */
export const WithError: Story = {
  args: { hint: "We only use this to sign you in.", error: "Enter a valid email address." },
};

/** Required field: marked both visually and for assistive technology. */
export const Required: Story = { args: { required: true } };

/** Disabled field. */
export const Disabled: Story = { args: { disabled: true, modelValue: "user@example.com" } };

/** Label hidden for compact toolbars — still exposed to screen readers. */
export const HiddenLabel: Story = {
  args: { label: "Search", labelHidden: true, type: "search", placeholder: "Search…" },
};
