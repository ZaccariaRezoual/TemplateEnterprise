import vue from "@vitejs/plugin-vue";
import { defineConfig } from "vitest/config";

/**
 * Test configuration of the appointments module frontend.
 *
 * The pages are tested against a mocked feature service: what is exercised is
 * the behaviour a person meets — the step they are on, the slot disappearing
 * under them, the button that must not be offered — rather than the
 * transport, which the SDK already types.
 */
export default defineConfig({
  plugins: [vue()],
  test: {
    environment: "jsdom",
    globals: true,
    include: ["src/**/*.spec.ts"],
  },
});
