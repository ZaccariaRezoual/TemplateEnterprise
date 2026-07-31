import vue from "@vitejs/plugin-vue";
import { defineConfig } from "vitest/config";

/**
 * Test configuration of the authorization module frontend.
 * The store and the directive are tested against a mocked API; real
 * enforcement is covered by the backend integration tests.
 */
export default defineConfig({
  plugins: [vue()],
  test: {
    environment: "jsdom",
    globals: true,
    include: ["src/**/*.spec.ts"],
  },
});
