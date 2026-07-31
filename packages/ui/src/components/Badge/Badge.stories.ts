import type { Meta, StoryObj } from "@storybook/vue3-vite";
import Badge from "./Badge.vue";

/** Storybook entry for {@link Badge}. */
const meta = {
  title: "Components/Badge",
  component: Badge,
  tags: ["autodocs"],
  args: { default: "Active" },
  argTypes: {
    variant: {
      control: "select",
      options: ["neutral", "success", "warning", "danger", "info"],
    },
  },
} satisfies Meta<typeof Badge>;

export default meta;

type Story = StoryObj<typeof meta>;

/** Default, meaning-free label. */
export const Neutral: Story = { args: { variant: "neutral", default: "Draft" } };

/** Positive outcome. */
export const Success: Story = { args: { variant: "success", default: "Active" } };

/** Needs attention. */
export const Warning: Story = { args: { variant: "warning", default: "Expiring" } };

/** Failure or destructive state. */
export const Danger: Story = { args: { variant: "danger", default: "Suspended" } };

/** Informational. */
export const Info: Story = { args: { variant: "info", default: "Beta" } };

/** Every variant together, to compare weight and contrast in both themes. */
export const AllVariants: Story = {
  render: () => ({
    components: { Badge },
    template: `
      <div class="flex flex-wrap gap-2">
        <Badge variant="neutral">Draft</Badge>
        <Badge variant="success">Active</Badge>
        <Badge variant="warning">Expiring</Badge>
        <Badge variant="danger">Suspended</Badge>
        <Badge variant="info">Beta</Badge>
      </div>
    `,
  }),
};
