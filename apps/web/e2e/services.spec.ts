import { expect, test, type Page } from "@playwright/test";

/**
 * The Services module, end to end: from an empty catalogue to a page a
 * visitor can read.
 *
 * The property under test is the one the module exists to guarantee — a draft
 * never reaches the showcase — plus the round trip that proves the two halves
 * are wired to each other: what an administrator publishes at `/admin/services`
 * is what an anonymous browser sees at `/services`.
 *
 * Requires the API to be running (see playwright.config.ts). The account is
 * the development bootstrap admin, which holds every permission.
 */

const ADMIN_EMAIL = "admin@example.com";
const ADMIN_PASSWORD = "Password123!";

/** Fails if the page scrolls horizontally — the one layout bug users cannot work around. */
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

async function signInAsAdmin(page: Page): Promise<void> {
  await page.goto("/login");
  await page.getByLabel("Email").fill(ADMIN_EMAIL);
  await page.getByLabel("Password").fill(ADMIN_PASSWORD);
  await page.getByRole("button", { name: "Sign in" }).click();
  await expect(page).toHaveURL(/\/admin\/dashboard$/);
}

/**
 * Fills the service form and saves.
 *
 * The title carries a unique suffix so runs never collide on the slug, which
 * is unique across the whole catalogue — archived entries included.
 */
async function createService(page: Page, title: string, published: boolean): Promise<void> {
  await page.goto("/admin/services/new");
  await page.getByLabel("Titolo").fill(title);
  await page.getByLabel("Riga di presentazione").fill("Una riga di presentazione.");
  await page.getByLabel("Descrizione").fill("Il testo lungo della pagina.");

  if (published) {
    await page.getByLabel("Pubblicato").check();
  }

  await page.getByRole("button", { name: "Salva" }).click();
  // Creating lands on the edit page of what was created.
  await expect(page).toHaveURL(/\/admin\/services\/[0-9a-f-]{36}$/);
}

test.describe("Services", () => {
  test("a published service reaches the showcase and a draft does not", async ({
    page,
    browser,
  }) => {
    const suffix = crypto.randomUUID().slice(0, 8);
    const publishedTitle = `Servizio pubblicato ${suffix}`;
    const draftTitle = `Servizio in bozza ${suffix}`;

    await signInAsAdmin(page);
    await createService(page, publishedTitle, true);
    await createService(page, draftTitle, false);

    // A separate context: no cookies, no token — a real visitor.
    const anonymous = await browser.newContext();
    const visitor = await anonymous.newPage();

    await visitor.goto("/services");
    await expect(visitor.getByRole("heading", { level: 1 })).toHaveText("Servizi");
    await expect(visitor.getByText(publishedTitle)).toBeVisible();

    // The failure this module exists to prevent.
    await expect(visitor.getByText(draftTitle)).toHaveCount(0);

    await visitor.getByText(publishedTitle).click();
    await expect(visitor).toHaveURL(/\/services\/servizio-pubblicato-/);
    await expect(visitor.getByRole("heading", { level: 1 })).toHaveText(publishedTitle);

    // The metadata comes from the DATA, not from the route: without it every
    // service would preview on Slack with the same generic title.
    await expect(visitor).toHaveTitle(new RegExp(publishedTitle));

    await anonymous.close();
  });

  test("an archived service leaves the showcase", async ({ page, browser }) => {
    const title = `Servizio ritirato ${crypto.randomUUID().slice(0, 8)}`;

    await signInAsAdmin(page);
    await createService(page, title, true);

    await page.goto("/admin/services");
    const row = page.getByRole("row", { name: new RegExp(title) });
    await row.getByRole("button", { name: "Archivia" }).click();
    await row.getByRole("button", { name: "Confermi?" }).click();

    const anonymous = await browser.newContext();
    const visitor = await anonymous.newPage();
    await visitor.goto("/services");

    await expect(visitor.getByText(title)).toHaveCount(0);

    await anonymous.close();
  });

  test.describe("Mobile (375px)", () => {
    test.use({ viewport: { width: 375, height: 667 } });

    test("neither the showcase nor its administration scrolls sideways", async ({ page }) => {
      await page.goto("/services");
      await expectNoHorizontalScroll(page);

      await signInAsAdmin(page);

      await page.goto("/admin/services");
      await expectNoHorizontalScroll(page);

      await page.goto("/admin/services/new");
      await expectNoHorizontalScroll(page);
    });
  });
});
