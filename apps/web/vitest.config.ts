import { fileURLToPath, URL } from "node:url";
import vue from "@vitejs/plugin-vue";
import { defineConfig } from "vitest/config";

/**
 * Unit and component test configuration.
 *
 * Kept separate from vite.config.ts so the test environment (jsdom, setup
 * file, coverage) never leaks into the production build. End-to-end tests run
 * under Playwright instead — see playwright.config.ts.
 */
export default defineConfig({
  plugins: [vue()],
  resolve: {
    alias: {
      "@": fileURLToPath(new URL("./src", import.meta.url)),
    },
  },
  test: {
    environment: "jsdom",
    globals: true,
    include: ["src/**/*.spec.ts"],
    setupFiles: ["./vitest.setup.ts"],
    coverage: {
      provider: "v8",
      include: ["src/**/*.{ts,vue}"],
      exclude: ["src/**/*.spec.ts", "src/types/**", "src/main.ts"],
    },
  },
});
