import { defaultSiteContent, provideSiteContent, type SiteContent } from "./content";

/**
 * How the public site reaches the rest of the application.
 *
 * The site must offer a way in — "sign in", or "private area" for someone
 * already signed in — but it must not import the Auth module to know which.
 * The host answers instead, so the public site keeps working in an
 * application assembled without authentication at all.
 */
export interface SiteLinks {
  /** Where the header's primary action leads. */
  entryPath: () => string;
  /** Label of that action, e.g. "Accedi" or "Area riservata". */
  entryLabel: () => string;
}

const defaultLinks: SiteLinks = {
  entryPath: () => "/login",
  entryLabel: () => "Accedi",
};

let links: SiteLinks = defaultLinks;

/** Integration seams the HOST exposes and this module plugs into. */
export interface SiteModuleHost {
  /**
   * The project's content. Omit it and the placeholder copy is used, which
   * says on screen that it is a placeholder.
   */
  content?: SiteContent | undefined;
  /** How the site links into the rest of the application. */
  links?: SiteLinks | undefined;
}

/**
 * Wires the Site module into the host application. Call once at bootstrap.
 *
 * The module exports `siteRoutes`, but whether the application serves a
 * public site at all is the host's decision: an internal tool simply does not
 * install this module, and its root goes straight to the private area.
 *
 * @param host The host integration seams.
 */
export function installSiteModule(host: SiteModuleHost = {}): void {
  provideSiteContent(host.content ?? defaultSiteContent);

  // Assignment, not a conditional update: installing must fully define the
  // module's state. A second install that silently kept the previous links
  // would make the outcome depend on call order.
  links = host.links ?? defaultLinks;
}

/**
 * Returns the configured entry links, for the header.
 *
 * @returns The host's links, or the defaults ("/login").
 */
export function useSiteLinks(): SiteLinks {
  return links;
}
