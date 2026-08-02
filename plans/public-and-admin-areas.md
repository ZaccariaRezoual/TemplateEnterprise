# Piano — Area pubblica e area admin

> Piano operativo per dividere l'applicazione in un **sito pubblico**
> (home, chi siamo, servizi, contatti) e un'**area riservata** accessibile solo
> agli utenti autenticati. Stessa logica di [PLAN.md](../PLAN.md): fasi
> ordinate per dipendenza, ognuna con un criterio di done verificabile.

---

## 1. Il problema

Oggi `apps/web` è un'applicazione interamente riservata: `/` reindirizza a
`/dashboard`, il layout è uno solo, e ogni rotta nasce con `requiresAuth`.

Un progetto reale costruito su questo template ha quasi sempre **due facce**:

- una **vetrina pubblica**, che chiunque raggiunge senza account;
- uno **strumento riservato**, dove si lavora.

Sono due prodotti con esigenze opposte: la vetrina deve essere leggera,
indicizzabile e curata visivamente; lo strumento deve essere dense di dati,
protetto e veloce da usare. Metterli nello stesso guscio produce un sito
pubblico con una barra di navigazione da gestionale, oppure un gestionale che
carica il footer di marketing a ogni pagina.

## 2. Decisione centrale: dove vive la parte pubblica

**Il sito pubblico è un modulo**: `modules/site/`.

Segue la regola del framework (serve ad almeno due progetti → `modules/`) e
porta con sé la proprietà che conta: **un progetto che è solo un gestionale lo
disabilita** (`Modules:Site:Enabled = false`) e ottiene l'applicazione di oggi,
senza rimuovere codice.

| Cosa                                              | Dove                                  |
| ------------------------------------------------- | ------------------------------------- |
| Pagine pubbliche, layout pubblico, form contatti  | `modules/site/`                       |
| Contenuti del singolo progetto (testi, sezioni)   | `apps/web/src/site.config.ts`         |
| Meccanismo delle due aree (shell, guardie, rotte) | `apps/web` — è il composition root    |
| Area riservata                                    | quello che esiste già, sotto `/admin` |

La separazione fra le ultime due righe è il punto: **il meccanismo appartiene
al framework, il contenuto al progetto**. Un progetto nuovo cambia
`site.config.ts` e i testi; non tocca né le guardie né il routing.

## 3. Rotte e shell

```
/                    → sito pubblico (home)
/about /services /contact
/login /register     → restano dove sono, sono del modulo Auth
/admin               → dashboard (era /dashboard)
/admin/users …       → tutta l'area riservata
```

Tre layout, selezionati da `meta.layout` (il meccanismo esiste già in
`App.vue`):

| Layout         | Da                              | Per            |
| -------------- | ------------------------------- | -------------- |
| `PublicLayout` | nuovo, nel modulo Site          | vetrina        |
| `AdminLayout`  | **rinomina** di `DefaultLayout` | area riservata |
| `BlankLayout`  | invariato                       | login, errori  |

La rinomina non è cosmetica: `DefaultLayout` diventerebbe il layout **non**
predefinito, e un nome che mente costa più di un rename.

### Due dettagli che si rompono e vanno cambiati insieme

1. **Il redirect post-login.** Oggi `installAuthGuard` manda a `{ path: "/" }`
   dopo il login e quando un utente autenticato apre `/login`. Con `/` che
   diventa la vetrina, un utente che accede finirebbe sul marketing. La
   destinazione post-login diventa **`/admin`**, e va resa configurabile dal
   host (un progetto potrebbe volere altro).
2. **`meta.title`.** Oggi il titolo del documento è `"Enterprise Framework"`
   di default. Per la vetrina serve il nome del prodotto e, sulle pagine
   pubbliche, una `description` — vedi §7.

## 4. Il punto delicato: cosa succede se il modulo Auth è spento

Questa è la parte del piano che merita più attenzione, perché oggi il
comportamento è **silenziosamente sbagliato**.

`meta.requiresAuth` è applicato dalla guardia che installa il modulo Auth
(`installAuthGuard`, chiamata da `installAuthModule`). Senza quel modulo, la
meta resta lì e **non fa niente**: l'area riservata si aprirebbe a chiunque.

L'API resta protetta — la fallback policy del host richiede un utente
autenticato, quindi non è una fuga di dati: le pagine si aprirebbero vuote,
piene di errori 401. Ma un guscio amministrativo che si apre a un anonimo è
comunque un difetto grave, e del tipo peggiore: nessun test fallisce.

**Decisione: fail closed.** Le rotte dell'area riservata vengono **registrate
solo se il modulo Auth è installato**. Una rotta che non esiste non può
aprirsi, e la voce di navigazione sparisce con lei.

Realizzazione: `installAuthModule` dichiara al host di avere installato la
guardia (un flag esposto dal modulo, non una variabile globale del host); la
composizione delle rotte in `apps/web/src/router/index.ts` include l'area
riservata solo in quel caso. Se il modulo manca, `/admin` risponde 404 lato
client e il sito pubblico resta perfettamente funzionante.

Va scritto un test che lo dimostri: **è l'unico modo perché la proprietà
sopravviva a un refactor**.

## 5. Contenuti: configurazione, non CMS

Le pagine pubbliche sono componenti Vue del modulo; i testi e le sezioni
arrivano da un unico file di progetto:

```ts
// apps/web/src/site.config.ts
export const site = {
  name: "Acme",
  claim: "…",
  sections: { … },
  contact: { email: "…", address: "…" },
};
```

Perché non un CMS: sarebbe la stessa scommessa dei moduli di business scartati
in Fase 8 di PLAN.md. Un CMS ha senso quando **il cliente** deve modificare i
testi senza sviluppatori; finché non è un requisito, aggiunge uno schema, una
UI di editing e una cache da invalidare per risolvere un problema che si
risolve con un file.

Il modulo Settings esiste già se un progetto vuole rendere modificabili poche
stringhe (indirizzo, email di contatto) senza deploy.

Le pagine restano **sostituibili**: un progetto che vuole una home tutta sua
scrive la propria e la registra al posto di quella del modulo. Il modulo
fornisce un punto di partenza, non una gabbia.

## 6. Il form contatti — l'unico pezzo con backend

`POST /api/site/contact`, e va progettato con cura perché è **l'unico endpoint
anonimo che accetta testo libero** dell'intera applicazione.

- `AllowAnonymous()` con commento motivato (regola del repository).
- **Rate limiting dedicato**, più stretto di quello globale e partizionato per
  IP: è la superficie di abuso.
- **Honeypot** (campo nascosto che i bot compilano) invece di un captcha: zero
  dipendenze, zero attrito per l'utente, ferma la maggior parte del traffico
  automatico. Un captcha si aggiunge se e quando i log mostrano che serve.
- Invio tramite `IEmailSender` del modulo Email — che di default **logga
  invece di inviare**, quindi in sviluppo funziona senza SMTP.
- Validazione lato server con FluentValidation, e lo stesso schema Zod lato
  client.
- **Persistenza opzionale**: salvare i messaggi significa creare uno schema e
  una schermata per leggerli. Alla prima iterazione l'email basta.

**Da non dimenticare** (non è codice, ma senza è un problema legale in UE): un
form che raccoglie nome ed email ha bisogno di un'informativa privacy
raggiungibile dalla pagina. Il template dovrebbe prevedere la rotta e un testo
segnaposto, non scrivere l'informativa al posto del progetto.

## 7. SEO: il limite dell'SPA, da decidere

`apps/web` è una SPA Vite: il server restituisce un HTML vuoto e il contenuto
compare dopo il JavaScript. Per un'area riservata è irrilevante. **Per una
vetrina è il punto centrale**: i crawler moderni eseguono JS, ma il risultato è
più lento, meno affidabile e peggiore per le anteprime social (che il JS non lo
eseguono affatto).

Tre strade:

| Opzione                             | Costo                         | Quando ha senso                                          |
| ----------------------------------- | ----------------------------- | -------------------------------------------------------- |
| **SPA così com'è**                  | zero                          | La vetrina è una brochure, il traffico arriva da altrove |
| **Prerender delle rotte pubbliche** | un plugin e uno step di build | Poche pagine statiche — **il caso tipico**               |
| SSR completo                        | un secondo runtime da gestire | Contenuti dinamici e molti                               |

**Raccomandazione: prerender** (`vite-plugin-prerender` o equivalente) in una
fase dedicata, dopo che le pagine esistono. In tutti e tre i casi servono
comunque i **meta tag per rotta** (title, description, Open Graph): quelli si
fanno subito, con un `useSeo()` che legge da `meta`.

Questa è una **decisione da confermare**, perché cambia la definizione di done
della vetrina.

## 8. Design system — due documenti, una sola base di token

Le due aree hanno linguaggi visivi legittimamente diversi: la vetrina respira
(sezioni a tutta larghezza, scala tipografica ampia, un hero), il gestionale
comprime (densità, tabelle, form). Un unico documento che descrive entrambi
diventa illeggibile per tutti e due i pubblici.

Quindi **due documenti**:

| Documento                      | Copre                                                                                                          |
| ------------------------------ | -------------------------------------------------------------------------------------------------------------- |
| `docs/design-system.md`        | **La base condivisa**: architettura dei token, contratto di accessibilità, convenzioni dei componenti, theming |
| `docs/design-system-admin.md`  | La superficie densa: densità, tabelle, form, navigazione di lavoro                                             |
| `docs/design-system-public.md` | La vetrina: ritmo verticale, hero, sezioni, immagini, CTA                                                      |

**Il vincolo che non cambia**: i due documenti descrivono _usi diversi degli
stessi token_, non due sistemi di token. Se ognuno definisse i propri colori e
i propri raggi, si perderebbe la proprietà per cui un progetto si ri-brandizza
modificando solo i token semantic — e questo template esiste in gran parte per
quella proprietà.

### Come si ottiene un aspetto davvero diverso senza rompere nulla

Le due aree possono sembrare prodotti diversi restando dentro l'architettura:
`PublicLayout` marca la radice con `data-surface="public"`, e un blocco di
override ridefinisce **solo i token semantic** dentro quella superficie —
esattamente il meccanismo con cui funziona il tema scuro.

```css
[data-surface="public"] {
  --semantic-...: ...; /* scala, ritmo, forma della vetrina */
}
```

Risultato: la vetrina può avere angoli, spaziature e scala tipografica tutte
sue, e un rebrand resta **una sola modifica ai token**. Un progetto che vuole
la vetrina identica al gestionale cancella il blocco.

Componenti (`Hero`, `Section`, `FeatureCard`, `CtaBand`): in `packages/ui`
**solo se** servono a più di una pagina; altrimenti restano nel modulo Site.

E il vincolo di sempre: **mobile-first, verifica a 375px**. La vetrina è la
pagina che riceve più traffico da telefono di qualunque altra.

## 9. Fasi

### Fase 0 — Le due aree (solo meccanismo)

- `DefaultLayout` → `AdminLayout`; `meta.layout` accetta `"public"`
- Area riservata sotto `/admin`, `dashboardRoutes` incluso
- Redirect post-login → `/admin`, configurabile dal host
- **Fail closed**: le rotte admin si registrano solo con il modulo Auth
  installato
- Aggiornamento di e2e e navigazione

✅ **Done quando**: `/admin` richiede il login; con il modulo Auth disattivato
`/admin` non esiste; la suite (unit + e2e) è verde con i nuovi percorsi.

### Fase 1 — Il modulo Site

- `modules/site/` con manifest, frontend, `PublicLayout`, `siteRoutes`
- Pagine: home, chi siamo, servizi, contatti (form non ancora funzionante),
  privacy (segnaposto)
- `apps/web/src/site.config.ts` con i contenuti del progetto
- `/` serve la home pubblica; con il modulo spento, `/` reindirizza a `/admin`

✅ **Done quando**: il sito pubblico si naviga da anonimo, l'area riservata
resta protetta, e disabilitare il modulo riporta l'applicazione a com'è oggi.

**Fatto.** `modules/site` con cinque pagine, `PublicLayout`, contenuti in
`apps/web/src/site.config.ts`. Il form contatti resta segnaposto fino alla
Fase 2, di proposito: una pagina che sembra finita e perde i messaggi è
peggio di una pagina che pubblica un indirizzo email.

### Fase 2 — Form contatti end-to-end

- `POST /api/site/contact`: anonimo, rate limit dedicato, honeypot, validazione
- Invio via `IEmailSender`, destinatario da configurazione
- Frontend: stato di invio, errore, conferma
- Test: unit (validazione, honeypot), integration (429, 202/200), e2e (invio)

✅ **Done quando**: un messaggio inviato dal browser compare nel log
dell'`IEmailSender` di sviluppo, e un secondo invio ravvicinato viene limitato.

**Fatto.** Endpoint anonimo con policy di rate limit propria e honeypot; Site
pubblica `ContactMessageReceived`, Email lo recapita — nessuno dei due
referenzia l'altro. Aggiunti alla gerarchia condivisa `RateLimitedError` (429
non aveva un tipo) e `Textarea` al design system.

### Fase 3 — Design system della vetrina

- **Definizione con la skill `ui-ux-pro-max`** del linguaggio visivo della
  vetrina (stile, scala, ritmo, gerarchia), da tradurre in token — non in
  valori letterali
- Override `data-surface="public"` sui soli token semantic
- Componenti `Hero`/`Section`/`FeatureCard`/`CtaBand` se giustificati
- **`docs/design-system-public.md`** e **`docs/design-system-admin.md`**, con
  `docs/design-system.md` che resta la base condivisa
- Storybook e verifica a 375 / 768 / 1440

✅ **Done quando**: la vetrina è costruita solo con token e componenti del
design system, e cambiare i token semantic la ri-brandizza — entrambe le aree
insieme, ciascuna con il proprio carattere.

### Fase 4 — SEO

- `useSeo()`: title, description, Open Graph per rotta
- `sitemap.xml` e `robots.txt` per le rotte pubbliche
- **Se confermato**: prerender delle rotte pubbliche in build

✅ **Done quando**: ogni pagina pubblica ha meta unici, e (con il prerender)
l'HTML servito contiene il testo senza eseguire JavaScript.

### Fase 5 — Documentazione

- `modules/site/README.md` con le decisioni
- Pattern page `wiki/patterns/add-public-page.md`
- Aggiornamento di `docs/create-project.md` (i contenuti da personalizzare
  subito) e della regola in `CLAUDE.md`

✅ **Done quando**: aggiungere una pagina pubblica si fa seguendo solo la
pattern page.

## 10. Cosa NON prevede questo piano

Per scelta, con lo stesso criterio della Fase 8 di PLAN.md — non costruire su
requisiti immaginari:

- **CMS o editor di contenuti** (§5)
- **Blog, portfolio, e-commerce**: sono prodotti, non parti di un template
- **Cookie banner e informativa**: la rotta e il segnaposto sì, il testo no —
  è materia legale del singolo progetto
- **Multi-lingua della vetrina** oltre a quello che il modulo Localization già
  offre
- **Un secondo runtime SSR** finché il prerender basta

## 11. Decisioni prese

1. **SEO** (§7): **prerender delle sole rotte pubbliche**, in Fase 4, quando le
   pagine sono definitive. Non SSR: un secondo runtime in produzione per
   quattro pagine statiche non si ripaga. I meta tag per rotta si fanno subito,
   perché servono in ogni scenario — le anteprime social non eseguono
   JavaScript.
2. **Prefisso dell'area riservata**: **`/admin`**. `/app` è ambiguo (tutto è
   «app»); `/admin` dice cosa c'è dietro e non richiede spiegazioni.
3. **Messaggi di contatto**: **solo email**. Persistere significa schema,
   migration, schermata di lettura e permesso: di fatto un mini-CRM. Un
   progetto lo aggiungerà quando saprà cosa gli serve.
4. **Design system**: **tre documenti** (base condivisa + admin + public), una
   sola base di token — vedi §8.

## 12. Ordine di esecuzione

```
Fase 0  Due aree (meccanismo)     tutto il resto vi si appoggia
Fase 1  Modulo Site               le pagine, prima del backend che le serve
Fase 2  Form contatti             l'unico pezzo con backend
Fase 3  Design system vetrina     si tokenizza ciò che esiste, non ciò che si immagina
Fase 4  SEO                       ha senso quando le pagine sono definitive
Fase 5  Documentazione            contestuale, non in fondo
```

La Fase 0 è deliberatamente **senza contenuti**: separare le due aree è una
modifica strutturale che tocca rotte, layout, guardie e test. Mescolarla con la
scrittura delle pagine renderebbe illeggibile il diff proprio dove serve
attenzione — cioè sul fail closed di §4.
