import type { Meta, StoryObj } from "@storybook/vue3-vite";
import Avatar from "./Avatar.vue";

/** Storybook entry for {@link Avatar}. */
const meta = {
  title: "Components/Avatar",
  component: Avatar,
  tags: ["autodocs"],
  args: { name: "Ada Lovelace" },
  argTypes: {
    size: { control: "select", options: ["sm", "md", "lg"] },
    decorative: { control: "boolean" },
  },
} satisfies Meta<typeof Avatar>;

export default meta;

type Story = StoryObj<typeof meta>;

/** No image: initials fallback. */
export const Initials: Story = {};

/** With a picture. */
export const WithImage: Story = {
  args: { src: "https://avatars.githubusercontent.com/u/1?v=4" },
};

/** Broken URL: renders exactly like the initials fallback at runtime. */
export const BrokenImage: Story = { args: { src: "https://example.invalid/missing.png" } };

/** All sizes together. */
export const Sizes: Story = {
  render: (args) => ({
    components: { Avatar },
    setup: () => ({ args }),
    template: `
      <div class="flex items-center gap-3">
        <Avatar v-bind="args" size="sm" />
        <Avatar v-bind="args" size="md" />
        <Avatar v-bind="args" size="lg" />
      </div>
    `,
  }),
};
