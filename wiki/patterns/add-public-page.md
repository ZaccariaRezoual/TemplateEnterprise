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

**Quando serve, ma altrove**: se la pagina mostra i **dati di un modulo** — il
catalogo dei servizi, un elenco di eventi — la pagina va nel modulo che
possiede quei dati, non qui. Chi possiede il dato possiede la sua
rappresentazione: vedi
[`modules/services`](../../modules/services/README.md), che porta con sé sia
l'amministrazione sia la vetrina. I passi 1–5 restano gli stessi, cambia solo
la cartella; il §"Pagine con indirizzo dinamico" in fondo copre il caso in cui
l'indirizzo stesso è un dato.

## Procedura

1. **Crea la pagina** — `modules/site/frontend/src/pages/<Nome>Page.vue`

   Componi con quello che c'è: `PageSection` (da `@enterprise/ui`) decide il
   ritmo verticale, `HighlightGrid` rende una lista di punti.

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

## Pagine con indirizzo dinamico

Una pagina di dettaglio (`/services/<slug>`) non si può elencare a mano: gli
slug sono righe di una tabella e cambiano senza che nessuno tocchi il codice.
Due aggiunte, entrambe piccole.

**Il titolo viene dai dati.** `meta.title` è una stringa fissa, decisa quando
il modulo viene importato, cioè prima che i dati esistano. La pagina lo
riscrive quando arrivano:

```ts
watch(
  () => service.data.value,
  (loaded) => {
    if (loaded !== undefined) {
      applyRuntimeSeo({ title: loaded.title, description: loaded.shortDescription });
    }
  },
  { immediate: true },
);
```

`applyRuntimeSeo` è il seam del modulo verso `setSeo` di
`apps/web/src/core/seo/applySeo.ts`, passato dall'host in `installXModule`. Un
modulo non tocca il `<head>` da solo: il nome del sito e la base canonica li
conosce solo l'host.

**Gli indirizzi li scopre il prerender.** In `apps/web/prerender.config.mjs`:

```js
export const dynamicPublicRoutes = [
  { endpoint: "/api/services", property: "slug", prefix: "/services" },
];
```

L'endpoint deve essere **anonimo** (il prerender non ha sessione) e restituire
una lista. Poi si costruisce con l'API raggiungibile:

```bash
PRERENDER_API_ORIGIN=http://localhost:5080 \
VITE_PUBLIC_BASE_URL=https://example.com \
pnpm --filter @enterprise/web build:static
```

Senza `PRERENDER_API_ORIGIN` — o con l'API spenta, che in CI è la norma — si
rendono solo le rotte statiche e lo script lo dice. Non fallisce: un deploy
fermo perché un database non era su è un esito peggiore di una sitemap senza
le pagine di dettaglio.

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

**Il prerender va in timeout su una pagina che legge dati.** Aspetta l'`h1`, e
la tua pagina lo mostra solo dopo la risposta dell'API. Due cause: l'API non è
raggiungibile dal server di preview (il proxy `/api` è configurato anche in
`preview` di `vite.config.ts`, verifica che l'API sia su), oppure lo stato di
errore della pagina non ha un `h1` — e allora aggiungilo, perché una pagina
senza titolo è rotta anche per un utente.

## Correlati

- [Scegliere il token giusto](use-design-tokens.md)
- [Checklist responsive](responsive-checklist.md)
- [Aggiungere una feature frontend](add-feature.md)
