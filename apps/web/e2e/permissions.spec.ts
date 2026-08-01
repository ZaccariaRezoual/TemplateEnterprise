import { expect, test } from "@playwright/test";

/**
 * End-to-end permission behavior through the real stack.
 *
 * A freshly registered account gets the default "User" role — granted by the
 * Authorization module reacting to Auth's registration event — which carries
 * no administration permissions. So the users area must be unreachable and
 * invisible, while the API independently refuses the call.
 */
test.describe("Permissions", () => {
  async function register(page: import("@playwright/test").Page): Promise<void> {
    await page.goto("/register");
    await page.getByLabel("Display name").fill("Perms User");
    // Random, not just a timestamp: parallel workers can land on the same
    // millisecond and collide on the unique email constraint.
    await page.getByLabel("Email").fill(`perms-${crypto.randomUUID()}@example.com`);
    await page.getByLabel("Password").fill("Str0ngPassphrase");
    await page.getByRole("button", { name: "Create account" }).click();
    await expect(page).toHaveURL(/\/dashboard$/);
  }

  test("hides navigation the user cannot open", async ({ page }) => {
    await register(page);

    // The nav entry is not rendered at all, so the menu never leads to a
    // forbidden page.
    await expect(page.getByRole("link", { name: "Users" })).toHaveCount(0);
  });

  test("redirects a direct visit to the forbidden page", async ({ page }) => {
    await register(page);

    await page.goto("/users");

    await expect(page).toHaveURL(/\/forbidden/);
    await expect(page.getByRole("heading", { level: 1 })).toContainText("do not have access");
  });

  test("the API refuses the call regardless of the UI", async ({ page }) => {
    await register(page);

    // The client-side guard is a convenience; this asserts the real boundary.
    const status = await page.evaluate(async () => {
      const response = await fetch("/api/users", { headers: { Accept: "application/json" } });
      return response.status;
    });

    // No Authorization header from a raw fetch: the endpoint denies.
    expect([401, 403]).toContain(status);
  });
});
