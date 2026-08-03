import vue from "@vitejs/plugin-vue";
import { defineConfig } from "vitest/config";

/**
 * Test configuration of the services module frontend.
 *
 * The pages are tested against a mocked feature service, so what is exercised
 * is the behaviour the user sees — the three states of a list, the rules of
 * the form — rather than the transport, which the SDK already types.
 */
export default defineConfig({
  plugins: [vue()],
  test: {
    environment: "jsdom",
    globals: true,
    include: ["src/**/*.spec.ts"],
  },
});
