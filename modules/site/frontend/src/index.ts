// Ships this module's RouteMeta augmentation (`publicSite`) to consumers.
import "./types/router";

/**
 * Public surface of `@enterprise/module-site`.
 *
 * The host consumes four things: `installSiteModule` at bootstrap,
 * `siteRoutes` in its route registry, `PublicLayout` in its layout map, and
 * the `SiteContent` type to write its own copy.
 */
export { default as PublicLayout } from "./layouts/PublicLayout.vue";
export { installSiteModule, useSiteLinks } from "./install";
export type { SiteLinks, SiteModuleHost } from "./install";
export { siteRoutes } from "./routes";
export { contactFormSchema } from "./validators/contact.validator";
export type { ContactFormValues } from "./validators/contact.validator";
export { defaultSiteContent, useSiteContent } from "./content";
export type { SiteContent, SiteHighlight, SitePage } from "./content";
