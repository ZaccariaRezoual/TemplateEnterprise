import { expect, test, type Page } from "@playwright/test";

/**
 * Smoke test of the full stack: browser → Vite proxy → API → PostgreSQL/Redis.
 * Requires the API to be running (see playwright.config.ts).
 */
/** The demo lives in the private area, so every test signs in first. */
async function openDemo(page: Page): Promise<void> {
  await page.goto("/register");
  await page.getByLabel("Display name").fill("Demo User");
  await page.getByLabel("Email").fill(`demo-${crypto.randomUUID()}@example.com`);
  await page.getByLabel("Password").fill("Str0ngPassphrase");
  await page.getByRole("button", { name: "Create account" }).click();
  await expect(page).toHaveURL(/\/admin\/dashboard$/);

  await page.goto("/admin/demo");
}

test.describe("Demo feature", () => {
  test("loads server state and echoes a text", async ({ page }) => {
    await openDemo(page);

    await expect(page).toHaveURL(/\/admin\/demo$/);
    await expect(page.getByRole("heading", { name: "Demo feature" })).toBeVisible();

    // Server state arrived through TanStack Query.
    await expect(page.getByText("pong")).toBeVisible();

    // Mutation round-trip: assert on the live region, not on any element
    // containing the text — the Pinia history also renders it.
    await page.getByLabel("Text to echo").fill("hello from playwright");
    await page.getByRole("button", { name: "Send" }).click();
    await expect(page.getByText("Server echoed")).toContainText("hello from playwright");

    // Client state kept the entry, independently of the server response.
    await page.getByRole("button", { name: "Show" }).click();
    await expect(page.getByRole("listitem")).toHaveText("hello from playwright");
  });

  test("shows the server validation message for empty input", async ({ page }) => {
    await openDemo(page);

    await page.getByLabel("Text to echo").fill(" ");
    await page.getByRole("button", { name: "Send" }).click();

    await expect(page.getByRole("textbox", { name: "Text to echo" })).toHaveAttribute(
      "aria-invalid",
      "true",
    );
  });

  test("switches theme without reloading", async ({ page }) => {
    await openDemo(page);

    await page.getByRole("radio", { name: "Dark" }).click();

    await expect(page.locator("html")).toHaveAttribute("data-theme", "dark");
  });
});
