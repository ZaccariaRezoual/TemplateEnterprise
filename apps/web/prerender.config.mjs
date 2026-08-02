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
