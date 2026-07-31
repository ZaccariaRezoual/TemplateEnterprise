import { defineConfig, devices } from "@playwright/test";

/**
 * End-to-end test configuration.
 *
 * Playwright starts the Vite dev server itself; the API must be running
 * separately (`dotnet run --project apps/api/src/Api`) because e2e tests
 * exercise the real stack, not mocks.
 */
export default defineConfig({
  testDir: "./e2e",
  fullyParallel: true,
  forbidOnly: Boolean(process.env["CI"]),
  retries: process.env["CI"] === undefined ? 0 : 2,
  reporter: process.env["CI"] === undefined ? "list" : "html",
  use: {
    baseURL: "http://localhost:5173",
    trace: "on-first-retry",
  },
  projects: [{ name: "chromium", use: { ...devices["Desktop Chrome"] } }],
  webServer: {
    command: "pnpm dev",
    url: "http://localhost:5173",
    reuseExistingServer: process.env["CI"] === undefined,
    timeout: 120_000,
  },
});
