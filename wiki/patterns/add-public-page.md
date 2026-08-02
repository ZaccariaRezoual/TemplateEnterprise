# Aggiungere una pagina pubblica

**Fonte normativa**: [`docs/design-system-public.md`](../../docs/design-system-public.md) ·
[`modules/site/README.md`](../../modules/site/README.md)
**Esempio nel codice**: `modules/site/frontend/src/pages/ServicesPage.vue`

## Quando serve

Quando aggiungi una schermata che chiunque deve poter vedere **senza account**:
una pagina di prodotto, i casi studio, le condizioni di servizio.

**Quando NON serve**: se la pagina mostra dati di qualcuno, appartiene all'area
riservata → [Aggiungere una feature frontend](add-feature.md). La differenza
non è di stile: è `meta.requiresAuth`, e decide anche in quale area finisce la
rotta.

## Procedura

1. **Crea la pagina** — `modules/site/frontend/src/pages/<Nome>Page.vue`

   Componi con quello che c'è: `PageSection` decide il ritmo verticale,
   `HighlightGrid` rende una lista di punti.

   ```vue
   <PageSection :title="content.faq.title" :intro="content.faq.intro" heading-level="h1">
     <HighlightGrid :items="content.faq.highlights" />
   </PageSection>
   ```

   `heading-level="h1"` sulla prima sezione: è il titolo della pagina, e da
   quello dipendono sia l'outline del documento sia la dimensione (`text-display`).

2. **Metti i testi nel contenuto**, non nel markup —
   `modules/site/frontend/src/content.ts` per il tipo,
   `apps/web/src/site.config.ts` per le parole del progetto.

   Una pagina con le frasi scritte dentro costringe ogni progetto a modificare
   il componente, ed è esattamente ciò che il modulo esiste per evitare.

3. **Registra la rotta** — `modules/site/frontend/src/routes.ts`

   ```ts
   {
     path: "/faq",
     name: "site-faq",
     component: () => import("./pages/FaqPage.vue"),
     meta: {
       title: "Domande frequenti",
       description: "Le risposte alle domande che riceviamo più spesso.",
       publicSite: true,
     },
   }
   ```

   Tre cose, tutte necessarie:
   - **niente `requiresAuth`** — è ciò che la tiene alla radice invece di
     spostarla sotto `/admin`;
   - **`publicSite: true`** — sceglie il guscio pubblico;
   - **`description`** — finisce nei risultati di ricerca e nell'anteprima dei
     link. È una delle due righe che vale la pena scrivere davvero.

4. **Aggiungila al prerender** — `apps/web/prerender.config.mjs`

   ```js
   export const publicRoutes = ["/", "/about", "/services", "/contact", "/privacy", "/faq"];
   ```

   **Il passo che si dimentica.** Una pagina assente da questa lista funziona
   benissimo nel browser e resta invisibile ai crawler e alle anteprime dei
   link — che non eseguono JavaScript. Niente fallisce: semplicemente la
   pagina non esiste per il resto del web.

5. **Se serve un link**, aggiungilo alla navigazione (`SiteHeader.vue`) o al
   footer. Non tutte le pagine lo meritano: le condizioni di servizio stanno
   bene nel footer, un prodotto sta nel menu.

## Come si verifica

```bash
pnpm --filter @enterprise/web dev          # a 375px, non solo sul laptop
pnpm --filter @enterprise/web test:e2e     # e2e/site.spec.ts
```

E la verifica che riguarda il prerender, che è l'unica a dire la verità sul
risultato pubblicato:

```bash
VITE_PUBLIC_BASE_URL=https://example.com pnpm --filter @enterprise/web build:static
grep -c "<h1" apps/web/dist/faq/index.html    # deve trovare il titolo
```

Se l'HTML statico non contiene il testo della pagina, per un crawler quella
pagina è vuota.

## Errori tipici

**La pagina finisce sotto `/admin`.** Ha `requiresAuth` nel meta: l'area di una
rotta si decide da lì.

**La pagina si apre ma con il guscio sbagliato** (header del gestionale):
manca `publicSite: true`.

**Il link condiviso su Slack mostra il titolo di un'altra pagina.** La rotta
non è in `prerender.config.mjs`, quindi l'unfurler ha letto l'`index.html`
generico.

**I titoli sono enormi anche nell'area riservata** (o viceversa minuscoli nella
vetrina). Hai usato `text-3xl` invece di `text-display`: le utility di ruolo
cambiano valore in base alla superficie, quelle di dimensione no.

**Il paragrafo occupa tutta la larghezza dello schermo.** Manca `max-w-prose`
— è l'errore di leggibilità più comune di una pagina di marketing.

## Correlati

- [Scegliere il token giusto](use-design-tokens.md)
- [Checklist responsive](responsive-checklist.md)
- [Aggiungere una feature frontend](add-feature.md)
