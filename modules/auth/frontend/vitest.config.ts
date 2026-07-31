import vue from "@vitejs/plugin-vue";
import { defineConfig } from "vitest/config";

/**
 * Test configuration of the auth module frontend. Store and guard logic are
 * tested against a mocked AuthApi; the real HTTP flow is covered by the
 * backend integration tests and the application's e2e suite.
 */
export default defineConfig({
  plugins: [vue()],
  test: {
    environment: "jsdom",
    globals: true,
    include: ["src/**/*.spec.ts"],
  },
});
