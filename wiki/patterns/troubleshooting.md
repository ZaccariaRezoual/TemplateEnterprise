# Errori che non hanno senso

Problemi **realmente incontrati** costruendo questo framework, il cui sintomo
non suggerisce la causa. Cerca il sintomo, non la spiegazione.

Se ne risolvi uno nuovo di questa specie — sintomo fuorviante, causa non
ovvia — aggiungilo qui: è il contenuto che fa risparmiare più tempo di
qualunque altro in questa wiki.

## Build e avvio

### `dotnet build` fallisce chiedendo un database

La build **avvia il host** per generare il documento OpenAPI, e in Development
le migrations partono all'avvio. Serve Postgres attivo
(`docker compose -f docker/docker-compose.yml up -d`), oppure
`Modules:AutoMigrate=false`.

### «The process cannot access the file … .dll» / MSB3027

Hai l'API in esecuzione: tiene i lock sulle dll. Fermala prima di ricompilare.
Il messaggio parla di file bloccati e non nomina mai il colpevole.

### La build fallisce per un warning

Voluto: gli analyzer .NET sono configurati come errori. Si sistema il codice,
non si abbassa la severità. Se il file è generato (migrations), è già escluso
in `.editorconfig`.

### `pnpm install` rifiuta la versione di Node

Il vincolo in `engines` è reale (`vue-router` 5 richiede `^22.18.0 ||

> =24.11.0`). Aggiorna Node; forzare l'installazione sposta il problema a
> runtime.

## Frontend che «non si vede»

### Un componente è invisibile, o largo zero pixel

**Tailwind non scansiona quel package.** Le classi non finiscono nel bundle e
non c'è nessun errore di build: i pulsanti icona collassano a 0×0, i padding
spariscono. Aggiungi il percorso alle direttive `@source` in
`apps/web/src/assets/styles/main.css`.

Attenzione alla forma del pattern: `@source ".../modules/*/frontend/src"` **non
funziona**. Serve `@source ".../modules/**/frontend/src/**/*.{vue,ts}"`.

Come confermarlo in dieci secondi, dalla console del browser:

```js
getComputedStyle(document.querySelector("[data-testid=…]")).padding;
// "0px" → è questo il problema
```

### Le classi del consumatore vengono ignorate

Il componente non fonde le classi con `cn()`: la sua `bg-primary` e la tua
`bg-surface` convivono e vince l'ordine del CSS, cioè il caso.

### In dark mode il contrasto crolla

Token semantic aggiunto solo in `:root` e non in `[data-theme="dark"]`, oppure
un token primitive usato direttamente nel markup.

## Rete e realtime

### Il realtime non arriva in sviluppo, senza errori

Il proxy Vite ha bisogno di `ws: true` sul path `/hubs`. Senza, la richiesta di
negotiate viene proxata ma **l'upgrade a WebSocket no**: il client degrada in
silenzio o fallisce senza dire perché.

### 429 mentre lavori o durante i test

Il rate limiter è partizionato **per IP**, e i worker Playwright condividono lo
stesso. I limiti sono già rilassati in `appsettings.Development.json`, e `/hubs`
è esentato — se qualcuno reintroduce il limite lì, gli e2e cominciano a fallire
in modo intermittente.

Nota: partizionare per IP significa che un intero ufficio dietro NAT condivide
il budget. Non è un bug, è un compromesso da conoscere.

### Ogni chiamata del frontend fallisce

L'API non è in esecuzione. Il dev server fa da proxy su `/api`, non serve dati.

## Contratti e SDK

### Il metodo dell'SDK c'è ma la risposta è `unknown`

L'handler backend dichiara `Task<IResult>`. Solo il tipo **concreto**
(`Ok<T>`, `NoContent`, `FileStreamHttpResult`) porta la forma della risposta nel
documento OpenAPI. È già successo due volte in questo repository, ed è
invisibile finché non scrivi il frontend.

### La CI dice «SDK is out of date»

Contratto cambiato senza rigenerare, o solo uno dei due file generati
committato. Servono **entrambi**: `apps/api/openapi/v1.json` e
`packages/sdk/src/generated/schema.ts`.

### Il file OpenAPI risulta sempre modificato dopo una build

Era prettier che lo riformattava in pre-commit mentre `dotnet build` lo
riscrive nel formato del generatore. È in `.prettierignore` — se ci finisce di
nuovo, il sintomo torna.

### Un enum arriva come numero

L'API serializza gli enum **per nome**. Se ne vedi uno numerico, quel tipo esce
da un percorso che non passa dalle opzioni JSON del host.

## Permessi e account

### 403 anche da amministratore

Il permesso non è in `Permissions.All`. Il ruolo `Admin` è costruito proprio da
quella lista: la costante esiste, l'endpoint la pretende, nessuno ce l'ha.

### Un permesso assegnato non ha effetto

I permessi viaggiano **dentro l'access token**: diventano effettivi al refresh
successivo (al massimo 15 minuti). Scelta deliberata — check stateless, zero
round-trip — non un bug.

### Non riesco ad amministrare niente su un database nuovo

Usa il bootstrap admin: **admin@example.com / Password123!** in Development. La
registrazione crea solo account `BasicUser`.

### Dopo un aggiornamento gli account hanno perso i ruoli

Il seeder **cancella i ruoli built-in non più dichiarati in codice** (e le
relative assegnazioni). È quello che ha permesso il rename
`Administrator` → `Admin` senza lasciare in giro il vecchio ruolo, ancora
capace di concedere tutti i permessi. In sviluppo si riassegnano; è documentato
in `modules/authorization/README.md`.

## Moduli

### Il `ModuleLoader` non trova il modulo

`module.json` non è embedded, o il `LogicalName` non è esattamente
`module.json`.

### Un evento non arriva a nessun subscriber

Due cause, entrambe silenziose:

1. il bus fa dispatch sul tipo **concreto**: pubblicare da un ciclo tipizzato
   `IDomainEvent` fa perdere il tipo;
2. il modulo del subscriber non registra i propri handler con `AddMediatR`.

### Il lavoro di avvio di un modulo gira «troppo presto»

Gli hosted service partono nell'ordine di caricamento dei moduli, che è un
grafo di **dipendenze**, non di seeding: un modulo senza dipendenze parte per
primo, prima che gli altri abbiano migrato il proprio schema. Se il tuo lavoro
ha bisogno che tutti siano pronti, agganciati a `ApplicationStarted` — vedi
`BootstrapAdminSeeder`.

### Una proiezione è vuota

Il modulo è stato installato dopo che gli eventi erano già passati, e gli
eventi **non vengono replayati**. Serve un backfill esplicito una tantum.

## Test

### Un test passa da solo e fallisce nella suite

Quasi sempre il rate limiter (vedi sopra). In seconda battuta: collisione di
dati fra worker paralleli — usa `crypto.randomUUID()`, mai un timestamp.

### Un e2e è instabile

Stai aspettando l'**assenza** di un elemento: è assente anche prima di
comparire, quindi l'attesa passa in uno stato sbagliato. Aspetta uno stato
esplicito, come `data-status="connected"`.

### `toBeInViewport` fallisce con «viewport ratio 0»

Hai scrollato prima che il contenuto asincrono fosse renderizzato: la pagina
era più corta. Aspetta la visibilità dell'elemento, **poi** scrolla.

### Un test di layout confronta bounding box e sbaglia

Le bounding box sono in coordinate **documento**, una barra `fixed` vive in
coordinate **viewport**. Chiedi al browser cosa è davvero dipinto in quel
punto: `document.elementFromPoint(x, y)`.

### Gli integration test falliscono all'avvio

Docker non è in esecuzione: usano Testcontainers, cioè un Postgres vero.

## Correlati

- [Primo giorno](../getting-started/day-one.md)
- [Testare il backend](test-backend.md) · [Testare il frontend](test-frontend.md)
