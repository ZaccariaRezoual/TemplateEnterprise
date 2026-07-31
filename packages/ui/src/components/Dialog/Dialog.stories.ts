import type { Meta, StoryObj } from "@storybook/vue3-vite";
import { ref } from "vue";
import Button from "../Button/Button.vue";
import Dialog from "./Dialog.vue";

/**
 * Storybook entry for {@link Dialog}.
 *
 * Stories drive the dialog from a trigger button rather than rendering it
 * permanently open: focus return only happens when there is something to
 * return focus to, and that is the behavior worth reviewing.
 */
const meta = {
  title: "Components/Dialog",
  component: Dialog,
  tags: ["autodocs"],
  args: { title: "Delete project" },
  argTypes: { persistent: { control: "boolean" } },
} satisfies Meta<typeof Dialog>;

export default meta;

type Story = StoryObj<typeof meta>;

/** Confirmation dialog with cancel and destructive actions. */
export const Confirmation: Story = {
  args: { description: "This permanently deletes the project and all its data." },
  render: (args) => ({
    components: { Button, Dialog },
    setup() {
      const open = ref(false);
      return { args, open };
    },
    template: `
      <div>
        <Button variant="danger" @click="open = true">Delete project</Button>
        <Dialog v-bind="args" v-model:open="open">
          Type the project name to confirm you understand the consequence.
          <template #footer>
            <Button variant="secondary" @click="open = false">Cancel</Button>
            <Button variant="danger" @click="open = false">Delete</Button>
          </template>
        </Dialog>
      </div>
    `,
  }),
};

/** Persistent: neither Escape nor a backdrop click closes it. */
export const Persistent: Story = {
  args: {
    title: "Migration in progress",
    description: "Closing now would leave the database in an inconsistent state.",
    persistent: true,
  },
  render: (args) => ({
    components: { Button, Dialog },
    setup() {
      const open = ref(false);
      return { args, open };
    },
    template: `
      <div>
        <Button @click="open = true">Start migration</Button>
        <Dialog v-bind="args" v-model:open="open">
          Applying schema changes…
          <template #footer>
            <Button variant="secondary" @click="open = false">Stop</Button>
          </template>
        </Dialog>
      </div>
    `,
  }),
};
