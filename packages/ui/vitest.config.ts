import { fileURLToPath, URL } from "node:url";
import vue from "@vitejs/plugin-vue";
import { defineConfig } from "vitest/config";

/**
 * Component test configuration for the design system.
 *
 * Components are tested in jsdom against their public contract — rendered
 * output, accessibility attributes and emitted events — never their internals,
 * so refactoring a component does not require rewriting its tests.
 */
export default defineConfig({
  plugins: [vue()],
  resolve: {
    alias: { "@": fileURLToPath(new URL("./src", import.meta.url)) },
  },
  test: {
    environment: "jsdom",
    globals: true,
    include: ["src/**/*.test.ts"],
  },
});
