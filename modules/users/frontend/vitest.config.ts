import vue from "@vitejs/plugin-vue";
import { defineConfig } from "vitest/config";

/**
 * Test configuration of the users module frontend.
 * The page is tested against a mocked API and a real permissions store, so
 * the permission-driven rendering is exercised end to end on the client.
 */
export default defineConfig({
  plugins: [vue()],
  test: {
    environment: "jsdom",
    globals: true,
    include: ["src/**/*.spec.ts"],
  },
});
