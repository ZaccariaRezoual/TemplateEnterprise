import type { Meta, StoryObj } from "@storybook/vue3-vite";
import Button from "./Button.vue";

/**
 * Storybook entry for {@link Button}.
 *
 * Stories double as the visual regression surface: every one of them renders
 * in both themes through the toolbar switcher, which is how we verify that
 * "change a token, change everything" still holds.
 */
const meta = {
  title: "Components/Button",
  component: Button,
  tags: ["autodocs"],
  args: { default: "Save changes" },
  argTypes: {
    variant: { control: "select", options: ["primary", "secondary", "ghost", "danger"] },
    size: { control: "select", options: ["sm", "md", "lg"] },
    disabled: { control: "boolean" },
    loading: { control: "boolean" },
    block: { control: "boolean" },
  },
} satisfies Meta<typeof Button>;

export default meta;

type Story = StoryObj<typeof meta>;

/** Default action button. */
export const Primary: Story = { args: { variant: "primary" } };

/** Supporting action of equal weight. */
export const Secondary: Story = { args: { variant: "secondary" } };

/** Low-emphasis action for dense UI. */
export const Ghost: Story = { args: { variant: "ghost" } };

/** Destructive action: always confirm before executing. */
export const Danger: Story = { args: { variant: "danger", default: "Delete account" } };

/** Operation in flight: interaction is blocked and `aria-busy` is set. */
export const Loading: Story = { args: { loading: true } };

/** Unavailable action. Prefer hiding actions the user can never perform. */
export const Disabled: Story = { args: { disabled: true } };

/** Every size side by side, to check optical alignment. */
export const Sizes: Story = {
  render: (args) => ({
    components: { Button },
    setup: () => ({ args }),
    template: `
      <div class="flex items-center gap-3">
        <Button v-bind="args" size="sm">Small</Button>
        <Button v-bind="args" size="md">Medium</Button>
        <Button v-bind="args" size="lg">Large</Button>
      </div>
    `,
  }),
};
