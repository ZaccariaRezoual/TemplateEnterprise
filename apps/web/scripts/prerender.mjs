#!/usr/bin/env node
/**
 * Prerenders the public routes into static HTML, and writes the sitemap.
 *
 * Why it exists: `apps/web` is a single-page app, so the server returns an
 * empty document and the content appears only after JavaScript runs. For the
 * private area that is irrelevant. For the public site it is the whole point —
 * link unfurlers (Slack, WhatsApp, LinkedIn) do not run JavaScript at all, and
 * a crawler that does still pays for it.
 *
 * How: the built app is served, a headless browser visits each public route,
 * and the rendered HTML is written back into `dist`. The browser is Playwright,
 * which the repository already uses for end-to-end tests — no new dependency,
 * and no Chromium download that CI has not already paid for.
 *
 * There is no hydration contract to honour: the app calls `createApp().mount()`,
 * which REPLACES the markup it finds. The static HTML serves crawlers and the
 * first paint; a moment later the live application takes over.
 *
 * Usage:
 *   pnpm --filter @enterprise/web build:static
 *
 * Environment:
 *   VITE_PUBLIC_BASE_URL   absolute site address; without it the sitemap and
 *                          the robots.txt Sitemap line are skipped, because
 *                          both require absolute URLs.
 */
import { chromium } from "@playwright/test";
import { existsSync, mkdirSync, readFileSync, writeFileSync } from "node:fs";
import { dirname, join, resolve } from "node:path";
import { fileURLToPath } from "node:url";
import { preview } from "vite";
import { publicRoutes } from "../prerender.config.mjs";

// It lives in apps/web, not in the repository's scripts/, because it depends
// on this app's own tooling (Vite and Playwright): a script at the root would
// resolve neither.
const webRoot = resolve(dirname(fileURLToPath(import.meta.url)), "..");
const distRoot = join(webRoot, "dist");
const baseUrl = (process.env["VITE_PUBLIC_BASE_URL"] ?? "").replace(/\/+$/, "");

if (!existsSync(join(distRoot, "index.html"))) {
  console.error("prerender: apps/web/dist is missing. Run the build first.");
  process.exit(1);
}

/** Where a route's HTML goes: "/" → dist/index.html, "/about" → dist/about/index.html. */
function outputPath(route) {
  return route === "/"
    ? join(distRoot, "index.html")
    : join(distRoot, route.replace(/^\//, ""), "index.html");
}

const server = await preview({
  root: webRoot,
  preview: { port: 0, strictPort: false },
  logLevel: "warn",
});

const origin = server.resolvedUrls?.local?.[0]?.replace(/\/+$/, "");
if (origin === undefined) {
  await server.close();
  throw new Error("prerender: the preview server did not report a URL.");
}

const browser = await chromium.launch();
const page = await browser.newPage();
let rendered = 0;

try {
  for (const route of publicRoutes) {
    await page.goto(`${origin}${route}`, { waitUntil: "networkidle" });

    // The heading is the proof the route actually rendered: without waiting
    // for it, a slow chunk would be captured as an empty shell — and the
    // failure would be silent, which is the worst kind for a build step.
    await page.waitForSelector("h1", { timeout: 10_000 });

    const html = await page.content();
    const target = outputPath(route);
    mkdirSync(dirname(target), { recursive: true });
    writeFileSync(target, html, "utf8");
    rendered += 1;
    console.log(`prerendered ${route} → ${target.replace(distRoot, "dist")}`);
  }
} finally {
  await browser.close();
  await server.close();
}

if (baseUrl === "") {
  console.log(
    `\nPrerendered ${rendered} routes. VITE_PUBLIC_BASE_URL is not set, so the sitemap was skipped.`,
  );
  process.exit(0);
}

const today = new Date().toISOString().slice(0, 10);
const urls = publicRoutes
  .map(
    (route) =>
      `  <url>\n    <loc>${baseUrl}${route}</loc>\n    <lastmod>${today}</lastmod>\n  </url>`,
  )
  .join("\n");

writeFileSync(
  join(distRoot, "sitemap.xml"),
  `<?xml version="1.0" encoding="UTF-8"?>\n<urlset xmlns="http://www.sitemaps.org/schemas/sitemap/0.9">\n${urls}\n</urlset>\n`,
  "utf8",
);

const robotsPath = join(distRoot, "robots.txt");
const robots = existsSync(robotsPath) ? readFileSync(robotsPath, "utf8") : "";
// A line that STARTS with the directive: `includes` matched the word inside
// the file's own comment and skipped the append — which is the kind of bug
// that only shows up when someone looks at the deployed file.
if (!/^Sitemap:/m.test(robots)) {
  writeFileSync(robotsPath, `${robots.trimEnd()}\nSitemap: ${baseUrl}/sitemap.xml\n`, "utf8");
}

console.log(`\nPrerendered ${rendered} routes, wrote sitemap.xml and the robots.txt Sitemap line.`);
