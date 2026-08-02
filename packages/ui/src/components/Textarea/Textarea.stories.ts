import type { Meta, StoryObj } from "@storybook/vue3-vite";
import Textarea from "./Textarea.vue";

/** Storybook entry for {@link Textarea}. */
const meta = {
  title: "Components/Textarea",
  component: Textarea,
  tags: ["autodocs"],
  args: { label: "Messaggio" },
} satisfies Meta<typeof Textarea>;

export default meta;

type Story = StoryObj<typeof meta>;

/** Default multi-line field. */
export const Default: Story = {};

/** With helper text under the field. */
export const WithHint: Story = { args: { hint: "Raccontaci cosa ti serve." } };

/** With a limit, which shows the live counter. */
export const WithLimit: Story = { args: { maxlength: 500 } };

/** Rejected value: the message replaces the hint and is announced. */
export const WithError: Story = {
  args: { hint: "Raccontaci cosa ti serve.", error: "Scrivi il tuo messaggio." },
};

/** Required, marked both visually and for assistive technology. */
export const Required: Story = { args: { required: true } };
