/**
 * Shape of the public site's content.
 *
 * The pages are framework code; the words are project code. A new project
 * fills this object once (`apps/web/src/site.config.ts`) and gets a complete
 * public site — without touching a component, a route or a guard.
 *
 * Deliberately NOT a CMS: a database-backed editor is worth its schema, its
 * editing UI and its cache only when the customer must change the text
 * without a developer. Until that is a requirement, a typed file is the
 * cheaper answer, and `SettingsReader` already covers the handful of values
 * (address, email) a project may want to change without a deploy.
 */

/** One entry of a list of selling points, services or values. */
export interface SiteHighlight {
  /** Short heading, a few words. */
  title: string;
  /** One or two sentences. Longer text belongs on a page of its own. */
  body: string;
}

/** A page made of an intro and a list of highlights. */
export interface SitePage {
  /** Title shown as the page heading and in the browser tab. */
  title: string;
  /** Sentence under the heading. */
  intro: string;
  /** Entries rendered as cards; an empty list renders just the intro. */
  highlights: readonly SiteHighlight[];
}

/** Everything the public site needs to render. */
export interface SiteContent {
  /** Product or company name, shown in the header and the footer. */
  name: string;
  /** One-line promise, shown as the hero heading. */
  claim: string;
  /** Supporting sentence under the claim. */
  intro: string;
  /** Label of the hero's primary action, which leads to the contact page. */
  callToAction: string;
  /** The "about us" page. */
  about: SitePage;
  /** The "services" page. */
  services: SitePage;
  /** The "contact" page; the form itself arrives in Fase 2. */
  contact: SitePage & {
    /** Public email address, shown while the form is not yet wired. */
    email: string;
    /** Optional postal address, rendered only when present. */
    address?: string | undefined;
  };
  /** Text of the privacy notice. A project MUST replace the placeholder. */
  privacy: SitePage;
}

/**
 * Placeholder content, used when the host does not provide its own.
 *
 * It exists so a freshly scaffolded project runs and looks finished, and so
 * that the text on screen says plainly that it is a placeholder — a lorem
 * ipsum that reads as real prose is how placeholder copy reaches production.
 */
export const defaultSiteContent: SiteContent = {
  name: "Enterprise Framework",
  claim: "Il punto di partenza dei nostri progetti",
  intro:
    "Questo è il sito pubblico incluso nel template. Sostituisci i testi in apps/web/src/site.config.ts.",
  callToAction: "Contattaci",
  about: {
    title: "Chi siamo",
    intro: "Contenuto segnaposto: descrivi qui la tua organizzazione.",
    highlights: [
      { title: "Cosa facciamo", body: "Segnaposto — sostituisci con la tua attività." },
      { title: "Come lavoriamo", body: "Segnaposto — sostituisci con il tuo metodo." },
      { title: "Perché noi", body: "Segnaposto — sostituisci con ciò che vi distingue." },
    ],
  },
  services: {
    title: "Servizi",
    intro: "Contenuto segnaposto: elenca qui i servizi offerti.",
    highlights: [
      { title: "Primo servizio", body: "Segnaposto — descrivi il servizio." },
      { title: "Secondo servizio", body: "Segnaposto — descrivi il servizio." },
      { title: "Terzo servizio", body: "Segnaposto — descrivi il servizio." },
    ],
  },
  contact: {
    title: "Contatti",
    intro: "Scrivici: rispondiamo il prima possibile.",
    highlights: [],
    email: "info@example.com",
  },
  privacy: {
    title: "Informativa privacy",
    intro:
      "Contenuto segnaposto. Un sito che raccoglie nome ed email deve pubblicare un'informativa reale: sostituisci questo testo prima di andare online.",
    highlights: [],
  },
};

let content: SiteContent = defaultSiteContent;

/**
 * Sets the content of the public site. Called once by `installSiteModule`.
 *
 * @param value The project's content.
 */
export function provideSiteContent(value: SiteContent): void {
  content = value;
}

/**
 * Returns the content of the public site.
 *
 * @returns The project's content, or the placeholder when none was provided.
 */
export function useSiteContent(): SiteContent {
  return content;
}
