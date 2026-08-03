/**
 * Public routes to prerender, and the only place that lists them.
 *
 * It is plain JavaScript because a Node script has to read it without a
 * TypeScript step. **Adding a public page means adding it here too** — the
 * pattern page `wiki/patterns/add-public-page.md` says so, because a page
 * missing from this list is invisible to crawlers and to link previews, and
 * nothing else would notice.
 *
 * Private routes are deliberately absent: they require a session, so a
 * prerender would capture the sign-in redirect, and search engines have no
 * business indexing them.
 */
export const publicRoutes = ["/", "/about", "/services", "/contact", "/privacy"];

/**
 * Public routes whose addresses are DATA, and where to ask for them.
 *
 * A service page lives at `/services/<slug>`, and the slugs are rows in a
 * database: there is nothing to write in the list above, and a list written
 * by hand would be wrong the first time someone adds a service. Each entry
 * names an API endpoint returning a list of objects, and the property of each
 * object that holds the path segment.
 *
 * The endpoint must be ANONYMOUS: the prerender has no session, and a page
 * that needed one would not be a public page.
 *
 * The API may well be unreachable at build time — it usually is, in CI. The
 * prerender then renders the static routes and says so, rather than failing:
 * a deploy that stops because a database was not up would be a worse outcome
 * than a sitemap missing its detail pages.
 *
 * @type {{ endpoint: string, property: string, prefix: string }[]}
 */
export const dynamicPublicRoutes = [
  { endpoint: "/api/services", property: "slug", prefix: "/services" },
];
