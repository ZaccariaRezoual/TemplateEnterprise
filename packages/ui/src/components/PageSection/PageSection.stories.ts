import type { Meta, StoryObj } from "@storybook/vue3-vite";
import PageSection from "./PageSection.vue";

/** Storybook entry for {@link PageSection}. */
const meta = {
  title: "Components/PageSection",
  component: PageSection,
  tags: ["autodocs"],
  args: { title: "Servizi", intro: "Quello che facciamo, in breve." },
} satisfies Meta<typeof PageSection>;

export default meta;

type Story = StoryObj<typeof meta>;

/** A section under a page title. */
export const Default: Story = {};

/** The page title itself: same component, correct outline. */
export const AsPageTitle: Story = { args: { headingLevel: "h1" } };

/** A band of pure content: no heading, no intro, same rhythm. */
export const ContentOnly: Story = {
  args: { title: undefined, intro: undefined },
  render: (args) => ({
    components: { PageSection },
    setup: () => ({ args }),
    template: `<PageSection v-bind="args"><p class="text-text-muted">Contenuto della banda.</p></PageSection>`,
  }),
};
