import vue from "@vitejs/plugin-vue";
import { defineConfig } from "vitest/config";

/** Test configuration of the site module frontend. */
export default defineConfig({
  plugins: [vue()],
  test: {
    environment: "jsdom",
    globals: true,
    include: ["src/**/*.spec.ts"],
  },
});
