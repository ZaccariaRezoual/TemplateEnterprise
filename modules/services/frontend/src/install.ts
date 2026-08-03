import type { ApiClient } from "@enterprise/sdk";
import { provideServicesApi } from "./api/services.api";

/**
 * Page metadata a page can set once its data has arrived.
 *
 * The route can only declare fixed strings, and the title of a service page
 * lives in the database — so without this seam every service shared on Slack
 * would preview with the same generic title.
 */
export interface RuntimeSeo {
  /** Page title, without the site name; the host appends it. */
  title: string;
  /** Meta description and link-preview text. */
  description: string;
}

/** Sets the document metadata at runtime. */
export type SetSeo = (seo: RuntimeSeo) => void;

/**
 * No-op default.
 *
 * Metadata is the host's concern — it owns the site name, the canonical base
 * URL and the tags — so a module that is installed without the seam simply
 * keeps the title the route declared, rather than reaching into the document
 * itself.
 */
const noopSetSeo: SetSeo = () => {};

let setSeoImpl: SetSeo = noopSetSeo;

/** Integration seams the HOST exposes and this module plugs into. */
export interface ServicesModuleHost {
  /** The application's configured SDK client. */
  api: ApiClient;
  /**
   * Origin of the API, used to build the public URL of an image. Omit it for
   * a same-origin deployment, which is the normal case.
   */
  apiOrigin?: string | undefined;
  /**
   * How the application sets document metadata at runtime. Omit it and the
   * detail pages keep the title their route declared.
   */
  setSeo?: SetSeo | undefined;
}

/**
 * Wires the Services module into the host application. Call once at
 * bootstrap.
 *
 * Installing it is also what makes the host serve the catalogue at
 * `/services` instead of the Site module's static page — that choice belongs
 * to the composition root, see `apps/web/src/router/index.ts`.
 *
 * @param host The host integration seams.
 */
export function installServicesModule(host: ServicesModuleHost): void {
  provideServicesApi(host.api, host.apiOrigin ?? "");

  // Assignment, not a conditional update: installing must fully define the
  // module's state, or a second install that silently kept the previous seam
  // would make the outcome depend on call order.
  setSeoImpl = host.setSeo ?? noopSetSeo;
}

/**
 * Sets the document metadata of the current page.
 *
 * @param seo Title and description read from the data that just arrived.
 */
export function applyRuntimeSeo(seo: RuntimeSeo): void {
  setSeoImpl(seo);
}
