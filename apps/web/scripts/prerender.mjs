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
 * Detail pages whose addresses live in the database (a service page is
 * `/services/<slug>`) cannot be listed by hand. They are DISCOVERED: the
 * script asks the API for them, using the entries in `dynamicPublicRoutes`.
 * When the API is unreachable — which in CI it usually is — it renders the
 * static routes and says so, because a deploy that stops for a database that
 * was not up is a worse outcome than a sitemap missing its detail pages.
 *
 * Usage:
 *   pnpm --filter @enterprise/web build:static
 *
 * Environment:
 *   VITE_PUBLIC_BASE_URL   absolute site address; without it the sitemap and
 *                          the robots.txt Sitemap line are skipped, because
 *                          both require absolute URLs.
 *   PRERENDER_API_ORIGIN   where to ask for the dynamic addresses, e.g.
 *                          "http://localhost:5080". Unset means "do not ask",
 *                          and only the static routes are rendered.
 */
import { chromium } from "@playwright/test";
import { existsSync, mkdirSync, readFileSync, writeFileSync } from "node:fs";
import { dirname, join, resolve } from "node:path";
import { fileURLToPath } from "node:url";
import { preview } from "vite";
import { dynamicPublicRoutes, publicRoutes } from "../prerender.config.mjs";

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

/**
 * Asks the API for the addresses of the pages whose slugs live in data.
 *
 * Never throws: an unreachable API, a non-200 answer or a malformed payload
 * all mean "no dynamic routes this time", reported on stdout. Failing the
 * build here would make a deploy depend on a database being up, which is a
 * dependency a static build should not have.
 *
 * @param apiOrigin Origin to ask, or an empty string to skip asking.
 * @returns The discovered routes, in the order the API returned them.
 */
async function discoverDynamicRoutes(apiOrigin) {
  if (apiOrigin === "") {
    console.log("prerender: PRERENDER_API_ORIGIN is not set, so only the static routes are built.");
    return [];
  }

  const discovered = [];

  for (const { endpoint, property, prefix } of dynamicPublicRoutes) {
    const url = `${apiOrigin}${endpoint}`;

    try {
      const response = await fetch(url, { headers: { Accept: "application/json" } });

      if (!response.ok) {
        console.warn(`prerender: ${url} answered ${response.status}; skipping its pages.`);
        continue;
      }

      const payload = await response.json();

      if (!Array.isArray(payload)) {
        console.warn(`prerender: ${url} did not return a list; skipping its pages.`);
        continue;
      }

      for (const item of payload) {
        const segment = item?.[property];
        // Only plain segments: a slug is generated server-side and cannot
        // contain a slash, so anything that does is not one — and would
        // write outside the directory it belongs to.
        if (typeof segment === "string" && segment !== "" && !segment.includes("/")) {
          discovered.push(`${prefix}/${segment}`);
        }
      }
    } catch (error) {
      console.warn(`prerender: ${url} is unreachable (${error.message}); skipping its pages.`);
    }
  }

  return discovered;
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

const dynamicRoutes = await discoverDynamicRoutes(
  (process.env["PRERENDER_API_ORIGIN"] ?? "").replace(/\/+$/, ""),
);
const routesToRender = [...publicRoutes, ...dynamicRoutes];

const browser = await chromium.launch();
const page = await browser.newPage();
let rendered = 0;

try {
  for (const route of routesToRender) {
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
const urls = routesToRender
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
