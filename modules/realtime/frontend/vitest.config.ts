import vue from "@vitejs/plugin-vue";
import { defineConfig } from "vitest/config";

/**
 * Test configuration of the realtime module frontend.
 * The service is tested against a fake hub connection: the value is in the
 * lifecycle and dispatch logic, not in SignalR's own transport.
 */
export default defineConfig({
  plugins: [vue()],
  test: {
    environment: "jsdom",
    globals: true,
    include: ["src/**/*.spec.ts"],
  },
});
