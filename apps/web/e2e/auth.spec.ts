import { expect, test } from "@playwright/test";

/**
 * End-to-end auth flows through the real stack: browser → Vite proxy → API →
 * PostgreSQL. Covers the guard redirect, registration with auto sign-in,
 * the Bearer-protected account page, session survival across a reload
 * (refresh cookie) and logout.
 */
test.describe("Auth module", () => {
  test("guards, registers, survives a reload and signs out", async ({ page }) => {
    // Random, not just a timestamp: parallel workers can land on the same
    // millisecond and collide on the unique email constraint.
    const email = `e2e-${crypto.randomUUID()}@example.com`;

    // Anonymous visit to a protected route → guard redirects to login,
    // remembering the destination.
    await page.goto("/account");
    await expect(page).toHaveURL(/\/login\?redirect=(%2F|\/)account/);

    // Register (registration signs in) and land back on the app.
    await page.getByRole("link", { name: "Create one" }).click();
    await page.getByLabel("Display name").fill("E2E User");
    await page.getByLabel("Email").fill(email);
    await page.getByLabel("Password").fill("Str0ngPassphrase");
    await page.getByRole("button", { name: "Create account" }).click();

    // Session is reflected in the header; the protected page now opens and
    // shows data fetched from the Bearer-protected /me endpoint.
    await page.getByTestId("nav-account").click();
    await expect(page.getByTestId("account-email")).toHaveText(email);

    // Reload: the access token lives in memory only, so this proves the
    // refresh-cookie restore path.
    await page.reload();
    await expect(page.getByTestId("account-email")).toHaveText(email);

    // Sign out: back to login, and the protected route is locked again.
    await page.getByTestId("sign-out").click();
    await expect(page).toHaveURL(/\/login/);
    await page.goto("/account");
    await expect(page).toHaveURL(/\/login\?redirect=(%2F|\/)account/);
  });

  test("rejects wrong credentials with a single generic message", async ({ page }) => {
    await page.goto("/login");

    await page.getByLabel("Email").fill("nobody@example.com");
    await page.getByLabel("Password").fill("WrongPassphrase1");
    await page.getByRole("button", { name: "Sign in" }).click();

    await expect(page.getByRole("alert")).toContainText("Invalid email or password");
    await expect(page).toHaveURL(/\/login/);
  });
});
