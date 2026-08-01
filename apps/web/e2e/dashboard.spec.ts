import { expect, test, type Page } from "@playwright/test";

/**
 * The dashboard is the landing page and the visible proof of the framework's
 * composition: its tiles come from whichever modules are installed and from
 * what the caller is allowed to see.
 *
 * Requires the API to be running (see playwright.config.ts).
 */

async function register(page: Page): Promise<void> {
  await page.goto("/register");
  await page.getByLabel("Display name").fill("Dashboard User");
  await page.getByLabel("Email").fill(`dash-${crypto.randomUUID()}@example.com`);
  await page.getByLabel("Password").fill("Str0ngPassphrase");
  await page.getByRole("button", { name: "Create account" }).click();
  await expect(page).toHaveURL(/\/dashboard$/);
}

test.describe("Dashboard", () => {
  test("is where signing in lands", async ({ page }) => {
    await register(page);

    await expect(page.getByRole("heading", { name: "Dashboard", level: 1 })).toBeVisible();
  });

  test("shows the tiles the caller is allowed to see, and no others", async ({ page }) => {
    await register(page);

    const widgets = page.getByRole("region", { name: "Dashboard widgets" });
    await expect(widgets).toBeVisible();

    // Notifications are the account's own data, so a plain user gets that tile.
    await expect(widgets.getByText("Unread", { exact: true })).toBeVisible();

    // Users and Audit require permissions this account does not hold. Their
    // providers return nothing, so the tiles are simply absent — the page is
    // still useful rather than showing a wall of "forbidden".
    await expect(widgets.getByText("Recent activity")).toHaveCount(0);
  });

  test("stays readable on a 375px phone", async ({ page }) => {
    await page.setViewportSize({ width: 375, height: 667 });
    await register(page);

    await expect(page.getByRole("region", { name: "Dashboard widgets" })).toBeVisible();

    const overflow = await page.evaluate(() => ({
      scrollWidth: document.documentElement.scrollWidth,
      clientWidth: document.documentElement.clientWidth,
    }));
    expect(overflow.scrollWidth).toBeLessThanOrEqual(overflow.clientWidth + 1);
  });
});
