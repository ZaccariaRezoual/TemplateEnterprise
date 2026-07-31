# CLAUDE.md — Enterprise Vue.NET Framework 2026

Guida operativa per Claude Code su questo repository. Leggila come contratto: ogni modifica al codice deve rispettare le regole qui sotto.

## Cos'è questo progetto

Framework enterprise monorepo (Vue 3 + .NET 10) che fa da base a tutti i futuri progetti aziendali (SaaS, CRM, ERP, dashboard, portali, ecc.). Non è un singolo prodotto: è una piattaforma a moduli componibili.

**Documenti sorgente di verità:**

- [Struttura.md](Struttura.md) — visione, architettura e linee guida complete. In caso di dubbio architetturale, vince questo file.
- [PLAN.md](PLAN.md) — piano di costruzione in 9 fasi (0–8) con criteri di "done" per fase.

## Come procedere a ogni richiesta

1. **Verifica lo stato di avanzamento reale**: guarda cosa esiste già nel repo (cartelle `apps/`, `packages/`, `modules/`) e confrontalo con le fasi di PLAN.md. Non fidarti della memoria: il repo è la fonte di verità sullo stato.
2. **Colloca la richiesta nel piano**: se l'utente chiede una funzionalità che appartiene a una fase futura, verifica che i prerequisiti della fase esistano; se mancano, segnalalo e proponi il percorso minimo.
3. **Rispetta l'ordine delle fasi** quando l'utente dice genericamente "procedi" / "continua": riprendi dalla prima fase non completata secondo i criteri di done in PLAN.md.
4. **Ogni nuova funzionalità riusabile va nel posto giusto**: se può servire ad almeno due progetti → `packages/` o `modules/`, non dentro `apps/`.
5. **Aggiorna la documentazione** (`docs/`, README dei moduli) contestualmente al codice, non dopo.
6. **Nessuna feature è completa senza test** (unit sempre; integration per il backend; e2e per i flussi principali).

## Regole architetturali non negoziabili

- **Feature-first**: il codice si organizza per funzionalità (`features/users/`), mai per tipologia (`components/`, `services/` globali).
- **Dependency Rule**: i livelli interni non conoscono quelli esterni. Domain non referenzia EF Core; Application non conosce PostgreSQL; la UI non conosce Axios.
- **Le Entity EF non escono mai dall'API**: verso il frontend viaggiano solo DTO/Contracts.
- **Il frontend consuma solo l'SDK** (`packages/sdk`) generato da OpenAPI, mai endpoint scritti a mano: la catena è componente → composable → feature service (`features/*/api/`) → SDK. Nessuna feature costruisce URL né tocca il trasporto; le preoccupazioni trasversali (correlation id, token, mappatura errori) vivono solo in `core/api/apiClient.ts`. L'SDK va rigenerato con `node scripts/generate-sdk.mjs` quando cambia un contratto: la CI fallisce se il generato committato è disallineato.
- **Stato**: Pinia solo per client state; TanStack Query per tutto ciò che viene dalle API. Mai dati server dentro Pinia.
- **Design tokens sempre**: mai colori/spacing hardcoded; il theming (light/dark/custom) passa dai token, non da modifiche ai componenti (vedi sezione "Design System — gestione").
- **Moduli indipendenti**: ogni modulo in `modules/<nome>/` ha `README.md`, `module.json` (name, version, dependencies, enabled), `frontend/`, `backend/`, `shared/`, `tests/`. Deve poter essere installato/disabilitato senza toccare altri moduli. **Il modulo Auth è il template canonico**: persistenza di proprietà del modulo (DbContext proprio, schema PostgreSQL dedicato, migrations proprie), frontend come package workspace (`@enterprise/module-<nome>`) che si aggancia ai seam esposti da core (mai il contrario), configurazione sotto `Modules:<Nome>:*`. Regole complete in `docs/modules.md`.
- **Endpoint autenticati di default**: la fallback policy del host richiede un caller autenticato; gli endpoint anonimi fanno opt-out esplicito con `AllowAnonymous()` motivato da un commento. Access token JWT solo in memoria sul client; refresh token solo in cookie httpOnly con rotazione e reuse detection.
- **Autorizzazione su permessi, mai su ruoli**: gli endpoint dichiarano `RequirePermission("resource.action")`, il frontend usa `usePermissions()`/`v-can`. I ruoli sono solo un raggruppamento amministrativo. I check lato client sono presentazione: **l'API è il confine di sicurezza**.
- **Dati di un altro modulo → proiezione, non join**: un modulo che ha bisogno dei dati di un altro si costruisce la propria proiezione sottoscrivendo i suoi eventi pubblici (vedi `modules/users`), e referenzia entità altrui per id, mai con foreign key su un altro schema.
- **Eventi cross-modulo = contratti pubblici**: vivono nel progetto `shared/` del publisher; i subscriber referenziano solo quello, mai l'implementazione. Se serve _comportamento_ e non notifica, il host definisce un extension point in `Application/Abstractions` (vedi `IUserClaimsEnricher`).
- **Migrations e seeding all'avvio solo in development** (`Modules:AutoMigrate`); in produzione sono uno step esplicito di release.
- **Infrastruttura esterna dietro un'astrazione del modulo che la possiede**: `IEmailSender` (Email), `IFileStorageProvider` (Storage). Il default è quello sicuro per lo sviluppo (email loggata invece che inviata, file su disco locale); i progetti registrano la propria implementazione dopo il modulo. Le impostazioni si leggono da `SettingsReader`, mai dalla tabella.
- **Event-Driven**: i moduli comunicano pubblicando eventi di dominio sull'Event Bus, senza conoscere i destinatari. Un evento diventa live per i client implementando `IRealtimeEvent` (in `Application/Abstractions`): dichiara audience e channel, e il modulo Realtime lo distribuisce senza che il publisher lo referenzi. Le feature non usano mai SignalR direttamente (componente → composable → RealtimeService → SignalR).
- **Hub SignalR sottili**: solo gestione connessioni e routing; la logica sta nei Service (`RealtimeDispatcher`). Gruppi assegnati esclusivamente server-side dal principal autenticato — nessun metodo lato client per unirsi a un gruppo — e connessioni autenticate via JWT (token in query string accettato **solo** sui path `/hubs`).
- **Gli eventi realtime non vengono replayati** alla riconnessione: ciò che deve sopravvivere a una disconnessione va persistito e rifetchato (per questo Notifications salva prima di pubblicare).
- **Tailwind non vede i workspace package**: ogni nuovo package o module frontend con markup va aggiunto alle direttive `@source` in `apps/web/src/assets/styles/main.css`, altrimenti le sue classi mancano dal bundle senza alcun errore di build.
- **Sicurezza di default**: JWT + refresh token, CORS configurabile, rate limiting, security headers, RBAC + PBAC, audit log. Ogni nuovo endpoint nasce protetto, non "da proteggere poi".
- **Errori**: usare la gerarchia condivisa (`ApplicationError`, `ValidationError`, `BusinessError`, `UnauthorizedError`, `ForbiddenError`, `NetworkError`) su entrambi i lati; backend → ProblemDetails.

## Stack e struttura

- **Frontend** (`apps/web`): Vue 3 + TypeScript strict, Vite, Tailwind CSS v4, Pinia, TanStack Query, Vue Router, Motion, VueUse, Zod, Heroicons. Test: Vitest + Playwright.
- **Backend** (`apps/api`): .NET 10, ASP.NET Core Minimal API, Clean Architecture (Api / Application / Domain / Infrastructure), EF Core + PostgreSQL, MediatR, FluentValidation, Serilog (→ Seq), Redis, OpenAPI.
- **Packages**: `ui` (Design System con Storybook), `sdk` (generato da OpenAPI), `shared`, `types`.
- **DevOps**: pnpm workspaces + solution .NET unica, Docker Compose (PostgreSQL, Redis, Seq), GitHub Actions, Husky, Commitlint, Dependabot, CodeQL.

Struttura frontend dentro `apps/web/src`: `app/` (bootstrap, DI), `core/` (http, auth, config, logger, errors, permissions, interceptors — cose che esistono una sola volta), `shared/`, `features/`, `layouts/`, `router/`, `plugins/`, `assets/`, `types/`.

## Convenzioni

- **Naming**: componenti PascalCase; funzioni camelCase; cartelle kebab-case; costanti SCREAMING_SNAKE_CASE; interfacce con prefisso `I` (`IUser`); enum PascalCase (`UserRole`).
- **Componenti UI** (`packages/ui`): ogni componente ha `Component.vue`, `Component.types.ts`, `Component.test.ts`, `Component.stories.ts`, `index.ts`.
- **Versioni delle dipendenze condivise**: dichiarate una sola volta nel `catalog:` di `pnpm-workspace.yaml`; i package scrivono `"typescript": "catalog:"`, mai un range proprio, così due package non possono divergere su major diverse dello stesso strumento.
- **Commit**: Conventional Commits (enforced da Commitlint). Lingua del codice e dei commit: inglese; conversazione con l'utente: italiano.
- **Branch**: `main` (produzione), `develop` (sviluppo), `feature/*`, `fix/*`, `release/*`, `hotfix/*`. Non committare mai direttamente su `main`.
- **CI su ogni PR**: install → lint → type-check → unit test → build → docker build → security scan → coverage. Una modifica che rompe la CI non è finita.

## Standard di Codifica (obbligatorio)

Ogni file generato è codice di produzione di un template: verrà copiato e mantenuto per anni da altri team. La qualità della documentazione non si sacrifica mai per ridurre le righe.

**Regola generale**: ogni classe, interfaccia, record, enum, metodo pubblico, proprietà pubblica, componente Vue, composable, store Pinia e service deve essere documentato. Nessuna funzione pubblica senza documentazione. La documentazione spiega: scopo, responsabilità, quando usarla, parametri, valore di ritorno, eccezioni (backend), effetti collaterali e dipendenze importanti (es. "usa esclusivamente l'SDK", "invalida le query X").

### Backend (.NET)

- Ogni classe inizia con uno `/// <summary>` dettagliato: cosa fa, elenco delle responsabilità, cosa NON fa (es. "non contiene logica di persistenza"). Vale per service, handler MediatR, validator, Hub, moduli `IModule`.
- Ogni metodo pubblico ha summary completo con `<param>`, `<returns>` e `<exception>` per ogni errore della gerarchia condivisa che può lanciare (`NotFoundException`, `ValidationError`, ecc.).
- Entity, DTO/Contracts e Options documentano il significato di ogni proprietà; gli enum documentano ogni valore. I Contracts sono la fonte dei commenti che finiranno nell'OpenAPI e quindi nell'SDK: scrivili pensando a chi consumerà l'SDK senza vedere il backend.

```csharp
/// <summary>
/// Gestisce le operazioni applicative del dominio Users.
///
/// Responsabilità: creazione, aggiornamento, eliminazione e recupero utenti.
/// Non contiene logica di persistenza (delegata al repository).
/// </summary>
public sealed class UserService : IUserService
```

### Frontend (Vue/TS)

- Ogni componente Vue inizia con un blocco commento: nome, cosa visualizza, elenco responsabilità, e la precisazione di cosa delega (es. "la logica applicativa è nei composable, il componente non contiene business logic").
- Ogni composable, store, service e funzione esportata ha un JSDoc: cosa espone, quali dipendenze usa (SDK, quali query invalida), eventuali effetti collaterali.
- Vale anche per `packages/ui`: il file `.types.ts` documenta ogni prop, emit e slot; il commento del componente spiega quando usarlo rispetto ad alternative simili (Dialog vs Modal).

```ts
/**
 * Gestisce le operazioni sugli utenti.
 *
 * Espone caricamento, creazione, aggiornamento, eliminazione e
 * invalidazione cache. Usa esclusivamente l'SDK generato da OpenAPI;
 * dopo ogni mutazione invalida le query `users`.
 */
export function useUsers() {}
```

### Commenti inline — la regola del "perché"

I commenti inline spiegano **il perché**, mai **il cosa**. Vietato `// incrementa il contatore`. Un commento inline è dovuto quando la logica non è immediatamente comprensibile o quando una scelta ha una motivazione non ovvia:

```ts
// Ordinamento lato client: il dataset è < 100 elementi,
// evitiamo una chiamata API aggiuntiva.
```

```csharp
// La validazione precede la transazione per evitare lock inutili sul DB.
```

Le funzioni private si commentano solo quando implementano logiche complesse.

### Obiettivo

Il codice deve essere auto-documentante: uno sviluppatore che apre un file per la prima volta deve capire cosa fa, perché esiste, come usarlo, quali dipendenze e responsabilità ha — senza leggere altre parti del progetto.

## Design System — gestione (`packages/ui`)

Il design system è l'unico punto di personalizzazione visiva: un nuovo progetto si ri-brandizza modificando **solo i token del tema**, mai i componenti o le feature. Ogni modifica che rompe questa proprietà è un bug architetturale.

### Architettura dei token (3 livelli)

1. **Primitive** — valori grezzi (`--color-blue-500`, `--spacing-4`, `--radius-md`). Solo il design system li conosce.
2. **Semantic** — significato, non valore (`--color-primary`, `--color-surface`, `--color-danger`, `--text-heading`). È il livello che i temi ridefiniscono: primitive → semantic è l'unica mappatura che cambia tra progetti/temi.
3. **Component** — token specifici quando servono (`--button-radius`, `--card-shadow`), definiti sempre in funzione dei semantic.

**Regola di consumo**: i componenti di `packages/ui` usano solo token semantic/component; `apps/` e `modules/` usano solo componenti del design system e token semantic. Nessuno, mai, referenzia un primitive o un valore letterale (`#3b82f6`, `16px`) fuori da `packages/ui`.

### Implementazione

- Token come **CSS variables** definite in `packages/ui` (es. `tokens/primitives.css`, `tokens/semantic.css`, `themes/light.css`, `themes/dark.css`), esposte a Tailwind v4 tramite `@theme` così che le utility (`bg-primary`, `text-danger`) risolvano sui token e non su valori fissi.
- **Theme Engine**: light/dark/custom = un file di override dei token semantic + attributo sul root (`data-theme`). Cambiare tema o brand non tocca alcun componente.
- **Personalizzazione per progetto**: un progetto che nasce dal template crea il proprio `themes/<brand>.css` (override dei semantic) e, se serve, ridefinisce i component token. Stop. Se per ottenere il risultato serve modificare un componente, il componente ha un buco di tokenizzazione: si sistema il componente nel framework, non nel progetto.

### Regole operative

- **Nuovo stile ricorrente → nuovo token semantic**, non un valore inline: se un colore/spacing serve in due punti, nasce come token.
- **Nuovo componente**: prima si verifica che tutti i suoi stili risolvano su token; niente merge se contiene valori hardcoded (vale anche per shadow, z-index, durate delle animazioni Motion).
- **Modifiche ai token semantic sono breaking** per tutti i progetti che usano il framework: rinominare o rimuovere un token richiede deprecazione documentata, non sostituzione silenziosa.
- **`docs/design-system.md`** (da creare in Fase 3) è il catalogo: elenco token con significato e uso previsto, temi disponibili, guida "come brandizzare un nuovo progetto". Va aggiornato a ogni token o componente aggiunto, come da regola sulla documentazione contestuale.
- Storybook deve permettere di visualizzare ogni componente in ogni tema: è il test visivo che la proprietà "cambio token = cambio ovunque" regge.

## Comandi (man mano che il repo cresce)

Dalla root: `pnpm install` e `dotnet build` devono sempre funzionare. `docker compose -f docker/docker-compose.yml up` avvia le dipendenze locali (PostgreSQL, Redis, Seq — Seq UI su http://localhost:5341). `dotnet run --project apps/api/src/Api` avvia l'API su http://localhost:5080 (health: `/health/ready`, OpenAPI: `/openapi/v1.json`). `pnpm --filter @enterprise/web dev` avvia il frontend su http://localhost:5173 con proxy `/api` verso l'API. Test: `dotnet test` (backend; gli integration usano Testcontainers, serve Docker attivo), `pnpm test` (frontend unit), `pnpm --filter @enterprise/web test:e2e` (Playwright, richiede l'API attiva in Development). Migrations di un modulo: `dotnet ef migrations add <Nome> --project modules/<nome>/backend --startup-project apps/api/src/Api` (tool in `.config/dotnet-tools.json`, `dotnet tool restore`). Se aggiungi script (`scripts/generate-sdk`, `scripts/create-project`, ecc.), documentali qui.

Requisito ambiente: Node `^22.18.0 || >=24.11.0` (vincolo reale delle dipendenze, dichiarato in `engines`).

## Cosa NON fare

- Non creare cartelle globali per tipologia (`src/components`, `src/services`) fuori da `shared/` e `core/`.
- Non aggiungere dipendenze pesanti senza motivarlo: il framework deve restare componibile e i moduli rimovibili.
- Non implementare scorciatoie "temporanee" che violano le regole sopra (Axios diretto, entity esposte, colori hardcoded): in un template ogni scorciatoia viene copiata in tutti i progetti futuri.
- Non anticipare fasi (es. Multi-Tenant, CQRS avanzato) se l'utente non lo chiede: le Enterprise Features (Fase 7) sono opt-in.
