import type { SiteContent } from "@enterprise/module-site";

/**
 * Content of the public site.
 *
 * **This is the file a new project edits first.** The pages, the layout and
 * the routing belong to the framework; the words belong here. Rewriting this
 * object gives a complete public site without touching a component.
 *
 * The text below is deliberately a placeholder that SAYS it is one: copy that
 * reads like real prose is how "lorem ipsum" reaches production.
 *
 * Before going live, two of these are not optional:
 * - `contact.email`, which is the only way a visitor can reach you until the
 *   contact form lands;
 * - `privacy`, because a site that collects a name and an email needs a real
 *   notice, and a placeholder is worse than none.
 */
export const siteContent: SiteContent = {
  name: "Enterprise Framework",
  claim: "La base dei nostri progetti",
  intro:
    "Questo è il sito pubblico incluso nel template: sostituisci i testi in apps/web/src/site.config.ts.",
  callToAction: "Contattaci",

  about: {
    title: "Chi siamo",
    intro: "Contenuto segnaposto: descrivi qui la tua organizzazione.",
    highlights: [
      {
        title: "Cosa facciamo",
        body: "Segnaposto — sostituisci con la tua attività.",
      },
      {
        title: "Come lavoriamo",
        body: "Segnaposto — sostituisci con il tuo metodo di lavoro.",
      },
      {
        title: "Perché noi",
        body: "Segnaposto — sostituisci con ciò che vi distingue.",
      },
    ],
  },

  // FALLBACK, non la pagina servita. Con il modulo Services installato — e in
  // questo template lo è — la rotta /services mostra il catalogo dai dati,
  // amministrabile da /admin/services. Questa sezione resta perché un
  // progetto che disinstalla quel modulo torna ad avere una pagina statica, e
  // averla già scritta è ciò che rende la disinstallazione una riga sola.
  // Vedi modules/services/README.md § "The /services collision".
  services: {
    title: "Servizi",
    intro: "Contenuto segnaposto: elenca qui i servizi che offri.",
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
