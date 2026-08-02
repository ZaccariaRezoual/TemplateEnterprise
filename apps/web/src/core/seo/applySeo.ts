import type { RouteLocationNormalizedGeneric } from "vue-router";

/**
 * Document metadata applied on every navigation: title, description, canonical
 * URL and the Open Graph tags social platforms read.
 *
 * It lives in `core/` because it exists exactly once and belongs to no
 * feature: routes declare `meta.title` and `meta.description`, and this is
 * what turns those declarations into tags.
 *
 * Why tags and not a component: crawlers and link unfurlers read the document
 * head, and several of them (the social ones in particular) do not run
 * JavaScript at all. That is also why the public routes are prerendered —
 * see `scripts/prerender.mjs`; these tags are what the prerender captures.
 */

/** Name of the site, appended to every page title. */
let siteName = "Enterprise Framework";

/** Absolute base URL, used for canonical and `og:url`. Empty in development. */
let baseUrl = "";

/**
 * Configures the values that cannot be derived from a route.
 *
 * @param options.siteName Product name appended to page titles.
 * @param options.baseUrl Absolute site URL, e.g. "https://acme.example".
 */
export function configureSeo(options: { siteName?: string; baseUrl?: string }): void {
  if (options.siteName !== undefined && options.siteName !== "") {
    siteName = options.siteName;
  }
  if (options.baseUrl !== undefined) {
    // Trailing slashes would produce "https://acme.example//about".
    baseUrl = options.baseUrl.replace(/\/+$/, "");
  }
}

/** Creates the tag if absent, then sets its content. */
function setMeta(selector: string, attribute: "name" | "property", key: string, value: string) {
  let tag = document.head.querySelector<HTMLMetaElement>(selector);

  if (tag === null) {
    tag = document.createElement("meta");
    tag.setAttribute(attribute, key);
    document.head.appendChild(tag);
  }

  tag.content = value;
}

/**
 * Applies the metadata of a route to the document head.
 *
 * Called by the router's navigation guard, so every page gets it — including
 * the ones nobody remembered to think about.
 *
 * @param route The route being navigated to.
 */
export function applySeo(route: RouteLocationNormalizedGeneric): void {
  const pageTitle = typeof route.meta.title === "string" ? route.meta.title : undefined;
  const title = pageTitle === undefined ? siteName : `${pageTitle} · ${siteName}`;
  const description = typeof route.meta.description === "string" ? route.meta.description : "";

  document.title = title;
  setMeta('meta[property="og:title"]', "property", "og:title", title);
  setMeta('meta[property="og:type"]', "property", "og:type", "website");
  setMeta('meta[property="og:site_name"]', "property", "og:site_name", siteName);
  setMeta('meta[name="twitter:card"]', "name", "twitter:card", "summary_large_image");

  if (description !== "") {
    setMeta('meta[name="description"]', "name", "description", description);
    setMeta('meta[property="og:description"]', "property", "og:description", description);
  }

  // Canonical and og:url only when the deployment knows its own address:
  // guessing it from `location` would publish the preview domain, or
  // localhost, as the canonical URL of the page.
  if (baseUrl === "") {
    return;
  }

  const url = `${baseUrl}${route.path}`;
  setMeta('meta[property="og:url"]', "property", "og:url", url);

  let canonical = document.head.querySelector<HTMLLinkElement>('link[rel="canonical"]');
  if (canonical === null) {
    canonical = document.createElement("link");
    canonical.rel = "canonical";
    document.head.appendChild(canonical);
  }
  canonical.href = url;
}
