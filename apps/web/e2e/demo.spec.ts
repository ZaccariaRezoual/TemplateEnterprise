import { expect, test } from "@playwright/test";

/**
 * Smoke test of the full stack: browser → Vite proxy → API → PostgreSQL/Redis.
 * Requires the API to be running (see playwright.config.ts).
 */
test.describe("Demo feature", () => {
  test("loads server state and echoes a text", async ({ page }) => {
    await page.goto("/demo");

    await expect(page).toHaveURL(/\/demo$/);
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
    await page.goto("/demo");

    await page.getByLabel("Text to echo").fill(" ");
    await page.getByRole("button", { name: "Send" }).click();

    await expect(page.getByRole("textbox", { name: "Text to echo" })).toHaveAttribute(
      "aria-invalid",
      "true",
    );
  });

  test("switches theme without reloading", async ({ page }) => {
    await page.goto("/demo");

    await page.getByRole("radio", { name: "Dark" }).click();

    await expect(page.locator("html")).toHaveAttribute("data-theme", "dark");
  });
});
