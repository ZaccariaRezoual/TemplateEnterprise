import type { Meta, StoryObj } from "@storybook/vue3-vite";
import Skeleton from "./Skeleton.vue";

/** Storybook entry for {@link Skeleton}. */
const meta = {
  title: "Components/Skeleton",
  component: Skeleton,
  tags: ["autodocs"],
  argTypes: {
    shape: { control: "select", options: ["text", "block", "circle"] },
  },
} satisfies Meta<typeof Skeleton>;

export default meta;

type Story = StoryObj<typeof meta>;

/** A line of body copy. */
export const Text: Story = { args: { shape: "text", width: "12rem" } };

/** An area, e.g. a chart or an image. */
export const Block: Story = { args: { shape: "block", width: "16rem", height: "8rem" } };

/** An avatar. */
export const Circle: Story = { args: { shape: "circle", width: "2.5rem" } };

/**
 * A realistic placeholder: skeletons are composed to match the layout they
 * replace, which is what keeps the page from shifting when data arrives.
 */
export const CardPlaceholder: Story = {
  render: () => ({
    components: { Skeleton },
    template: `
      <div class="w-72 rounded-(--card-radius) border border-border bg-surface p-5">
        <Skeleton width="6rem" />
        <Skeleton shape="block" width="4rem" height="2rem" class="mt-3" />
        <Skeleton width="9rem" class="mt-3" />
      </div>
    `,
  }),
};
