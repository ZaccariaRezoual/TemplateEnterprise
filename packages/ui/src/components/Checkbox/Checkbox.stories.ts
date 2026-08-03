import type { Meta, StoryObj } from "@storybook/vue3-vite";
import Checkbox from "./Checkbox.vue";

/** Storybook entry for {@link Checkbox}. */
const meta = {
  title: "Components/Checkbox",
  component: Checkbox,
  tags: ["autodocs"],
  args: { label: "Pubblicato" },
} satisfies Meta<typeof Checkbox>;

export default meta;

type Story = StoryObj<typeof meta>;

/** Default unchecked box. */
export const Default: Story = {};

/** Checked. */
export const Checked: Story = { args: { modelValue: true } };

/** With the consequence of ticking it spelled out under the label. */
export const WithHint: Story = {
  args: { hint: "I visitatori lo vedranno nella vetrina." },
};

/** Rejected value: the message replaces the hint and is announced. */
export const WithError: Story = {
  args: { hint: "I visitatori lo vedranno.", error: "Serve almeno un'opzione." },
};

/** Disabled, still readable — the token keeps it above the contrast floor. */
export const Disabled: Story = { args: { disabled: true, modelValue: true } };
