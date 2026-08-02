import { expect, test, type Page } from "@playwright/test";

/**
 * The public site: what an anonymous visitor sees.
 *
 * The property under test is not "the pages render" — it is that the public
 * face and the private area stay separate: everything here must open without
 * a session, and nothing here may expose the private one.
 *
 * Requires the API to be running (see playwright.config.ts).
 */

const PUBLIC_PATHS = ["/", "/about", "/services", "/contact", "/privacy"];

async function expectNoHorizontalScroll(page: Page): Promise<void> {
  const overflow = await page.evaluate(() => ({
    scrollWidth: document.documentElement.scrollWidth,
    clientWidth: document.documentElement.clientWidth,
  }));

  expect(
    overflow.scrollWidth,
    `page scrolls horizontally: ${overflow.scrollWidth}px in ${overflow.clientWidth}px`,
  ).toBeLessThanOrEqual(overflow.clientWidth + 1);
}

test.describe("Public site", () => {
  test("every page opens without a session", async ({ page }) => {
    for (const path of PUBLIC_PATHS) {
      await page.goto(path);

      // Not merely "did not redirect to login": the page must have rendered
      // its heading, or an empty shell would pass.
      await expect(page).toHaveURL(new RegExp(`${path === "/" ? "/$" : path}`));
      await expect(page.getByRole("heading", { level: 1 })).toBeVisible();
    }
  });

  test("the root is the public home, not the private area", async ({ page }) => {
    await page.goto("/");

    await expect(page).toHaveURL(/localhost:5173\/$/);
    await expect(page.getByTestId("site-entry")).toBeVisible();
  });

  test("offers a way in without revealing the private area", async ({ page }) => {
    await page.goto("/");

    await page.getByTestId("site-entry").click();

    await expect(page).toHaveURL(/\/login$/);
  });

  test("the privacy notice is reachable from every page", async ({ page }) => {
    // A site collecting a name and an email needs a reachable notice; putting
    // it in the footer is what makes "every page" true.
    for (const path of ["/", "/contact"]) {
      await page.goto(path);
      await page.getByRole("link", { name: "Informativa privacy" }).click();
      await expect(page).toHaveURL(/\/privacy$/);
    }
  });

  test.describe("on a 375px phone", () => {
    test.use({ viewport: { width: 375, height: 667 } });

    test("no page scrolls horizontally", async ({ page }) => {
      for (const path of PUBLIC_PATHS) {
        await page.goto(path);
        await expectNoHorizontalScroll(page);
      }
    });

    test("navigation opens on tap and closes after navigating", async ({ page }) => {
      await page.goto("/");

      const toggle = page.getByTestId("site-menu-toggle");
      await expect(toggle).toBeVisible();
      await toggle.click();

      const menu = page.locator("#site-menu");
      await expect(menu).toBeVisible();

      await menu.getByRole("link", { name: "Servizi" }).click();
      await expect(page).toHaveURL(/\/services$/);
      // Left open, it would cover the page the visitor just asked for.
      await expect(menu).toBeHidden();
    });
  });
});
