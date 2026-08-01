import { expect, test, type Page } from "@playwright/test";

/**
 * Responsive guarantees, asserted at real device sizes.
 *
 * These exist because responsive regressions are invisible to every other
 * test in the suite: assertions on roles and text pass perfectly while the
 * page scrolls sideways and half the controls are too small to tap. Only a
 * measurement catches that.
 *
 * 375px is the reference small phone (iPhone SE / mini class). Anything that
 * works there works on larger phones.
 */

/** Fails if the page scrolls horizontally — the one layout bug users cannot work around. */
async function expectNoHorizontalScroll(page: Page): Promise<void> {
  const overflow = await page.evaluate(() => ({
    scrollWidth: document.documentElement.scrollWidth,
    clientWidth: document.documentElement.clientWidth,
  }));

  // 1px of tolerance for sub-pixel rounding; anything more is a real overflow.
  expect(
    overflow.scrollWidth,
    `page scrolls horizontally: ${overflow.scrollWidth}px content in ${overflow.clientWidth}px viewport`,
  ).toBeLessThanOrEqual(overflow.clientWidth + 1);
}

async function register(page: Page): Promise<void> {
  await page.goto("/register");
  await page.getByLabel("Display name").fill("Responsive User");
  await page.getByLabel("Email").fill(`rsp-${crypto.randomUUID()}@example.com`);
  await page.getByLabel("Password").fill("Str0ngPassphrase");
  await page.getByRole("button", { name: "Create account" }).click();
  await expect(page).toHaveURL(/\/dashboard$/);
}

test.describe("Mobile (375px)", () => {
  test.use({ viewport: { width: 375, height: 667 } });

  test("no page scrolls horizontally", async ({ page }) => {
    for (const path of ["/login", "/register", "/demo"]) {
      await page.goto(path);
      await expectNoHorizontalScroll(page);
    }

    await register(page);
    await expectNoHorizontalScroll(page);

    await page.goto("/nope-does-not-exist");
    await expectNoHorizontalScroll(page);
  });

  test("primary navigation moves to a reachable bottom bar", async ({ page }) => {
    await register(page);

    const bottomNav = page.getByTestId("mobile-nav");
    await expect(bottomNav).toBeVisible();

    // It must actually sit at the bottom of the viewport, not merely exist.
    const box = await bottomNav.boundingBox();
    expect(box).not.toBeNull();
    expect(box!.y + box!.height).toBeGreaterThan(600);
  });

  test("the last control is not covered by the bottom bar", async ({ page }) => {
    await register(page);
    // The demo page is the longest one in the app, so it is where a fixed bar
    // actually has content to cover.
    await page.goto("/demo");

    const target = page.getByTestId("notify-me");
    // Wait for the page to be fully laid out before measuring: scrolling to a
    // height that async content has not produced yet lands short of the end.
    await expect(target).toBeVisible();

    // Scroll to the very bottom: that is where a fixed bar overlaps content,
    // and where reserved padding either works or does not.
    await page.evaluate(() => globalThis.scrollTo(0, document.documentElement.scrollHeight));
    await expect(target).toBeInViewport();

    // Comparing bounding boxes would be wrong — they are document
    // coordinates while a fixed bar lives in viewport ones. Asking the
    // browser what is actually painted at the control's centre answers the
    // real question: can the user tap it?
    const topmost = await target.evaluate((element) => {
      const rect = element.getBoundingClientRect();
      const hit = document.elementFromPoint(rect.x + rect.width / 2, rect.y + rect.height / 2);
      return hit === null
        ? "nothing"
        : hit === element || element.contains(hit)
          ? "the control"
          : (hit.closest("[data-testid]")?.getAttribute("data-testid") ?? hit.tagName);
    });

    expect(topmost).toBe("the control");
  });

  test("the header keeps identity and status reachable", async ({ page }) => {
    await register(page);

    // Nav labels move out of the header, but the bell and account stay.
    await expect(page.getByTestId("notification-bell")).toBeVisible();
    await expect(page.getByTestId("nav-account")).toBeVisible();
  });
});

test.describe("Touch devices", () => {
  // The touch traits only, not a whole device preset: spreading one would
  // also set `defaultBrowserType`, which Playwright forbids inside a describe.
  // `isMobile` is what makes the browser report `pointer: coarse`, which is
  // the condition the touch-target rules are written against.
  test.use({ viewport: { width: 390, height: 844 }, hasTouch: true, isMobile: true });

  test("interactive controls meet the 44px touch minimum", async ({ page }) => {
    await page.goto("/login");

    // Enforced by `pointer: coarse`, so it applies to this emulated phone but
    // not to a desktop mouse layout.
    const submit = page.getByRole("button", { name: "Sign in" });
    const box = (await submit.boundingBox())!;
    expect(box.height).toBeGreaterThanOrEqual(44);

    const emailBox = (await page.getByLabel("Email").boundingBox())!;
    expect(emailBox.height).toBeGreaterThanOrEqual(44);
  });

  test("tapping works without hover", async ({ page }) => {
    await page.goto("/register");
    await page.getByLabel("Display name").fill("Touch User");
    await page.getByLabel("Email").fill(`touch-${crypto.randomUUID()}@example.com`);
    await page.getByLabel("Password").fill("Str0ngPassphrase");

    // tap(), not click(): proves nothing depends on a hover state first.
    await page.getByRole("button", { name: "Create account" }).tap();

    await expect(page).toHaveURL(/\/dashboard$/);
  });
});

test.describe("Tablet and desktop", () => {
  test("tablet keeps the layout intact", async ({ page }) => {
    await page.setViewportSize({ width: 768, height: 1024 });
    await register(page);
    await expectNoHorizontalScroll(page);
  });

  test("desktop shows inline navigation instead of the bottom bar", async ({ page }) => {
    await page.setViewportSize({ width: 1440, height: 900 });
    await register(page);

    await expect(page.getByTestId("mobile-nav")).toBeHidden();
    await expect(page.getByRole("link", { name: "Demo" })).toBeVisible();
    await expectNoHorizontalScroll(page);
  });
});
