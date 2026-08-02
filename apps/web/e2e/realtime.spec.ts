import { expect, test, type Page } from "@playwright/test";

/**
 * Realtime behavior through the whole stack: browser → SignalR → API → event
 * bus → back over the socket.
 *
 * These are the assertions that make Fase 6 real. Everything else about
 * realtime can be unit-tested; only this proves that a fact created on the
 * server reaches an already-open page with no refresh and no polling.
 */
test.describe("Realtime", () => {
  async function register(page: Page): Promise<string> {
    const email = `rt-${crypto.randomUUID()}@example.com`;
    await page.goto("/register");
    await page.getByLabel("Display name").fill("Realtime User");
    await page.getByLabel("Email").fill(email);
    await page.getByLabel("Password").fill("Str0ngPassphrase");
    await page.getByRole("button", { name: "Create account" }).click();
    await expect(page).toHaveURL(/\/admin\/dashboard$/);

    // The demo page owns the control that triggers a server-sent notification.
    await page.goto("/admin/demo");
    return email;
  }

  test("a notification reaches an open page without a refresh", async ({ page }) => {
    await register(page);

    // Nothing has arrived yet: no badge.
    await expect(page.getByTestId("notification-badge")).toHaveCount(0);

    await page.getByTestId("notify-me").click();

    // The badge appears without any navigation or reload. If realtime were
    // broken this would only pass after a manual reload, so the assertion is
    // deliberately made on the live page.
    await expect(page.getByTestId("notification-badge")).toHaveText("1");
    await expect(page.getByTestId("toast")).toContainText("Realtime works");
  });

  test("the notification is persisted, not just pushed", async ({ page }) => {
    await register(page);
    await page.getByTestId("notify-me").click();
    await expect(page.getByTestId("notification-badge")).toHaveText("1");

    // A push that was never stored would vanish here.
    await page.reload();

    await expect(page.getByTestId("notification-badge")).toHaveText("1");
    await page.getByTestId("notification-bell").click();
    await expect(page.getByRole("region", { name: "Notifications" })).toContainText(
      "Realtime works",
    );
  });

  test("two tabs of the same account both receive it", async ({ browser }) => {
    // Same account, two connections: the server targets the user group, so
    // every open tab must be updated, not just the one that acted.
    const context = await browser.newContext();
    const first = await context.newPage();
    await register(first);

    const second = await context.newPage();
    await second.goto("/admin/demo");
    await expect(second.getByTestId("notification-bell")).toBeVisible();
    // Wait for the second tab's socket to actually be up. Events are not
    // replayed on connect, so triggering before it is connected would test
    // nothing. Asserting on the explicit status beats waiting for the warning
    // to disappear: an absent element is also absent before it would ever
    // appear, so that wait can pass while still disconnected.
    await expect(second.getByTestId("connection-status")).toHaveAttribute(
      "data-status",
      "connected",
    );

    await first.getByTestId("notify-me").click();

    await expect(second.getByTestId("notification-badge")).toHaveText("1");
    await context.close();
  });

  test("another account does not receive it", async ({ browser }) => {
    const listenerContext = await browser.newContext();
    const listener = await listenerContext.newPage();
    await register(listener);

    const senderContext = await browser.newContext();
    const sender = await senderContext.newPage();
    await register(sender);

    await sender.getByTestId("notify-me").click();
    await expect(sender.getByTestId("notification-badge")).toHaveText("1");

    // A notification is personal: the audience is one user's connections.
    // Waiting a moment makes the absence meaningful rather than a race.
    await listener.waitForTimeout(1500);
    await expect(listener.getByTestId("notification-badge")).toHaveCount(0);

    await listenerContext.close();
    await senderContext.close();
  });

  test("shows no connection warning while the socket is healthy", async ({ page }) => {
    await register(page);

    // A permanent "connected" badge is noise; the indicator only appears when
    // live updates have actually stopped.
    await expect(page.getByTestId("connection-indicator")).toHaveCount(0);
  });
});
