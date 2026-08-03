import { defineConfig, devices } from "@playwright/test";

/**
 * End-to-end test configuration.
 *
 * Playwright starts the Vite dev server itself; the API must be running
 * separately because e2e tests exercise the real stack, not mocks:
 *
 * ```bash
 * Modules__Auth__CredentialsPermitLimit=1000 dotnet run --project apps/api/src/Api
 * ```
 *
 * **That environment variable is not optional for a full run.** Every test
 * signs in, and the session refreshes while it works; the default credential
 * budget is deliberately tight — it is a brute-force defence — and the whole
 * suite exhausts it in about a minute. The symptom is misleading: tests that
 * pass alone start failing in the suite with "element not found", because the
 * session silently stopped refreshing and the admin pages went away.
 *
 * Raising the limit is safe here and nowhere else: it is a throwaway
 * development host talking to a throwaway database.
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
