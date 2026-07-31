import type { Preview } from "@storybook/vue3-vite";
import "./preview.css";

/**
 * Global Storybook configuration.
 *
 * The theme toolbar is the point: it flips `data-theme` on the preview
 * document, so every story can be inspected in light and dark WITHOUT the
 * stories knowing anything about theming. A component that looks wrong after
 * the switch has hardcoded a value instead of using a semantic token.
 */
const preview: Preview = {
  parameters: {
    controls: { matchers: { color: /(background|color)$/i, date: /Date$/i } },
    a11y: { test: "error" },
  },

  globalTypes: {
    theme: {
      description: "Design system theme",
      toolbar: {
        title: "Theme",
        icon: "circlehollow",
        items: [
          { value: "light", title: "Light", icon: "sun" },
          { value: "dark", title: "Dark", icon: "moon" },
        ],
        dynamicTitle: true,
      },
    },
  },

  initialGlobals: { theme: "light" },

  decorators: [
    (story, context) => {
      const theme = context.globals["theme"] === "dark" ? "dark" : "light";
      document.documentElement.dataset["theme"] = theme;
      return {
        components: { story },
        template: '<div class="bg-background p-6 text-text"><story /></div>',
      };
    },
  ],
};

export default preview;
