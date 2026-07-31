import { fileURLToPath, URL } from "node:url";
import tailwindcss from "@tailwindcss/vite";
import vue from "@vitejs/plugin-vue";
import type { StorybookConfig } from "@storybook/vue3-vite";

/**
 * Storybook configuration for the design system.
 *
 * Storybook is the showcase AND the visual verification surface: every
 * component is rendered in both themes from the toolbar, which is how we check
 * that changing a token changes everything and that no component hardcodes a
 * value.
 */
const config: StorybookConfig = {
  stories: ["../src/**/*.stories.ts"],
  addons: ["@storybook/addon-docs", "@storybook/addon-a11y"],
  framework: { name: "@storybook/vue3-vite", options: {} },
  viteFinal: (viteConfig) => ({
    ...viteConfig,
    // The Vue plugin is declared explicitly: relying on the framework preset to
    // inject it left .vue files unprocessed in the production build, which
    // surfaced as parse errors instead of a clear "missing plugin".
    plugins: [...(viteConfig.plugins ?? []), vue(), tailwindcss()],
    resolve: {
      ...viteConfig.resolve,
      alias: {
        ...viteConfig.resolve?.alias,
        "@": fileURLToPath(new URL("../src", import.meta.url)),
      },
    },
  }),
};

export default config;
