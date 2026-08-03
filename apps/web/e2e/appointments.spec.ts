import { expect, test, type Page } from "@playwright/test";

/**
 * Booking, end to end, from a phone-sized viewport.
 *
 * The property under test is the round trip the plan asks for: a visitor
 * picks a day and an hour, signs in at the LAST step, and finds the result in
 * their own list — described as a request, because nobody has accepted it
 * yet.
 *
 * It runs at 375px throughout, not as a separate mobile test, because booking
 * is something people do from a phone: if the flow only works on a laptop it
 * does not work.
 *
 * Requires the API to be running (see playwright.config.ts).
 */

const ADMIN_EMAIL = "admin@example.com";
const ADMIN_PASSWORD = "Password123!";

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

async function signIn(page: Page, email: string, password: string): Promise<void> {
  await page.goto("/login");
  await page.getByLabel("Email").fill(email);
  await page.getByLabel("Password").fill(password);
  await page.getByRole("button", { name: "Sign in" }).click();
  await expect(page).toHaveURL(/\/admin\//);
}

/** Opens every day, so the test never depends on which weekday it runs on. */
async function openEveryDay(page: Page): Promise<void> {
  await page.goto("/admin/appointments/availability");

  for (const day of [
    "Lunedì",
    "Martedì",
    "Mercoledì",
    "Giovedì",
    "Venerdì",
    "Sabato",
    "Domenica",
  ]) {
    const checkbox = page.getByLabel(day, { exact: true });
    if (!(await checkbox.isChecked())) {
      await checkbox.check();
    }
  }

  await page.getByRole("button", { name: "Salva" }).click();
  await expect(page.getByText("Orari salvati.")).toBeVisible();
}

/** Publishes a bookable service and returns its id and slug. */
async function publishBookableService(page: Page, title: string): Promise<string> {
  await page.goto("/admin/services/new");
  await page.getByLabel("Titolo").fill(title);
  await page.getByLabel("Riga di presentazione").fill("Un'ora per mettere a fuoco il problema.");
  await page.getByLabel("Durata (minuti)").fill("60");
  await page.getByLabel("Pubblicato").check();
  await page.getByLabel("Prenotabile").check();
  await page.getByRole("button", { name: "Salva" }).click();

  await expect(page).toHaveURL(/\/admin\/services\/[0-9a-f-]{36}$/);
  return page.url().split("/").pop()!;
}

test.describe("Booking (375px)", () => {
  test.use({ viewport: { width: 375, height: 667 } });

  test("a visitor books from the service page and finds the request in their list", async ({
    page,
    browser,
  }) => {
    const title = `Consulenza ${crypto.randomUUID().slice(0, 8)}`;

    await signIn(page, ADMIN_EMAIL, ADMIN_PASSWORD);
    await openEveryDay(page);
    const serviceId = await publishBookableService(page, title);

    // A separate context: a real visitor with no session.
    const context = await browser.newContext({ viewport: { width: 375, height: 667 } });
    const visitor = await context.newPage();

    // The showcase leads into the booking flow, which is the link that
    // actually matters — not a URL typed by the test.
    await visitor.goto("/services");
    await visitor.getByText(title).click();
    await expect(visitor.getByRole("heading", { level: 1 })).toHaveText(title);
    await expectNoHorizontalScroll(visitor);

    await visitor.getByRole("link", { name: "Prenota questo servizio" }).click();
    await expect(visitor).toHaveURL(new RegExp(`/book/.*serviceId=${serviceId}`));
    await expectNoHorizontalScroll(visitor);

    // Step 1 — the day. The first offered one, whichever it is.
    await visitor.getByRole("button").filter({ hasText: /\d/ }).first().click();

    // Step 2 — the hour.
    await expect(visitor.getByText("2. Ora")).toBeVisible();
    await visitor
      .getByRole("button")
      .filter({ hasText: /^\d{2}:\d{2}$/ })
      .first()
      .click();

    // Step 3 — only NOW is an account asked for.
    await expect(visitor.getByText("3. Conferma")).toBeVisible();
    await expect(visitor.getByRole("link", { name: "Accedi o registrati" })).toBeVisible();
    await expectNoHorizontalScroll(visitor);

    // Register, then come back and finish. The slot survives the detour.
    const email = `booker-${crypto.randomUUID()}@example.com`;
    await visitor.goto("/register");
    await visitor.getByLabel("Display name").fill("Booking Visitor");
    await visitor.getByLabel("Email").fill(email);
    await visitor.getByLabel("Password").fill("Str0ngPassphrase");
    await visitor.getByRole("button", { name: "Create account" }).click();
    await expect(visitor).toHaveURL(/\/admin\/dashboard$/);

    await visitor.goto(`/book/${title.toLowerCase().replace(/\s+/g, "-")}?serviceId=${serviceId}`);
    await visitor.getByRole("button").filter({ hasText: /\d/ }).first().click();
    await visitor
      .getByRole("button")
      .filter({ hasText: /^\d{2}:\d{2}$/ })
      .first()
      .click();

    await visitor.getByLabel("Telefono").fill("+39 333 1234567");
    await visitor.getByRole("button", { name: "Invia richiesta" }).click();

    // The wording is the assertion: a request, never a confirmation.
    await expect(visitor.getByRole("heading", { level: 1 })).toHaveText("Richiesta inviata");
    await expect(visitor.getByText("Non è ancora una conferma")).toBeVisible();

    await visitor.goto("/admin/my-appointments");
    await expect(visitor.getByText(title)).toBeVisible();
    await expect(visitor.getByText("In attesa di conferma")).toBeVisible();
    await expectNoHorizontalScroll(visitor);

    await context.close();
  });

  test("the calendar and the opening hours do not scroll sideways", async ({ page }) => {
    await signIn(page, ADMIN_EMAIL, ADMIN_PASSWORD);

    await page.goto("/admin/appointments");
    await expect(page.getByRole("heading", { level: 1 })).toHaveText("Appuntamenti");
    await expectNoHorizontalScroll(page);

    await page.goto("/admin/appointments/availability");
    await expect(page.getByRole("heading", { level: 1 })).toHaveText("Orari di apertura");
    await expectNoHorizontalScroll(page);
  });
});
