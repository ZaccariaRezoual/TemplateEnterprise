import type { Meta, StoryObj } from "@storybook/vue3-vite";
import Card from "./Card.vue";

/** Storybook entry for {@link Card}. */
const meta = {
  title: "Components/Card",
  component: Card,
  tags: ["autodocs"],
  args: { title: "Monthly revenue", default: "Content of the card." },
  argTypes: {
    headingLevel: { control: "select", options: ["h2", "h3", "h4"] },
    flush: { control: "boolean" },
  },
} satisfies Meta<typeof Card>;

export default meta;

type Story = StoryObj<typeof meta>;

/** Title only. */
export const Default: Story = {};

/** Title plus supporting description. */
export const WithDescription: Story = {
  args: { description: "Compared with the same period last year." },
};

/** With a footer holding the card's action. */
export const WithFooter: Story = {
  render: (args) => ({
    components: { Card },
    setup: () => ({ args }),
    template: `
      <Card v-bind="args">
        Content of the card.
        <template #footer><span class="text-sm text-text-muted">Updated 2 minutes ago</span></template>
      </Card>
    `,
  }),
};

/** No inner padding: for tables and media that manage their own spacing. */
export const Flush: Story = { args: { flush: true } };
