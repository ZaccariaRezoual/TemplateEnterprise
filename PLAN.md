# Piano di Costruzione — Enterprise Vue.NET Framework 2026

> Piano operativo derivato da [Struttura.md](Struttura.md). Le fasi sono ordinate per dipendenza: ogni fase produce qualcosa di funzionante e verificabile prima di passare alla successiva.

---

## Fase 0 — Fondamenta del Repository (Monorepo)

Obiettivo: repository navigabile, tooling condiviso funzionante, prima pipeline CI verde.

1. **Inizializzazione**
   - `git init`, `.gitignore`, `.editorconfig`, `README.md`
   - Struttura cartelle: `apps/`, `packages/`, `modules/`, `docs/`, `docker/`, `scripts/`, `tools/`, `.github/`
2. **Gestione monorepo**
   - `pnpm` workspaces (`pnpm-workspace.yaml`) per il lato JS/TS
   - Solution `.sln` unica per il lato .NET
3. **Qualità e convenzioni**
   - ESLint + Prettier condivisi (config in `tools/` o package dedicato)
   - Husky + Commitlint (Conventional Commits)
   - `.editorconfig` + analyzer .NET (`Directory.Build.props`, nullable, warnings as errors)
4. **CI iniziale (GitHub Actions)**
   - Workflow PR: install → lint → type-check → build (anche se ancora vuoto)
   - Dependabot + CodeQL
5. **Docker Compose base**
   - Servizi: PostgreSQL, Redis, Seq
   - File `docker/docker-compose.yml` + `.env.example`

✅ Done quando: `pnpm install` e `dotnet build` funzionano dalla root, la CI passa su una PR, `docker compose up` avvia Postgres/Redis/Seq.

---

## Fase 1 — Backend Skeleton (`apps/api`)

Obiettivo: API .NET 10 con Clean Architecture, pronta a ospitare moduli.

1. **Progetti .NET**
   ```
   apps/api/
     src/
       Api/              (Minimal API, composition root)
       Application/      (use case, MediatR, FluentValidation)
       Domain/           (entità, value objects, eventi di dominio)
       Infrastructure/   (EF Core, Redis, servizi esterni)
     tests/
       UnitTests/
       IntegrationTests/
   ```
   - Rispettare la Dependency Rule: Domain non referenzia nulla, Application referenzia solo Domain, ecc.
2. **Cross-cutting**
   - Serilog (console + Seq), correlation id, request logging
   - Gestione errori centralizzata → ProblemDetails con gerarchia `ApplicationError` / `ValidationError` / `BusinessError` / `UnauthorizedError` / `ForbiddenError`
   - OpenAPI con documentazione automatica
   - Health checks (`/health`) per Postgres e Redis
   - Configurazione tipizzata per ambiente (Options pattern)
3. **Persistence**
   - EF Core + PostgreSQL, migrations, `DbContext` di base
   - Pattern: le Entity non escono mai dall'API → DTO/Contracts dedicati
4. **Sicurezza di base**
   - Security headers, CORS configurabile, Rate limiting
   - Predisposizione JWT (implementazione completa in Fase 4)
5. **Sistema di moduli backend**
   - Convenzione `IModule` (registrazione servizi + endpoint per modulo)
   - Caricamento moduli da configurazione (lettura `module.json`, flag `enabled`)
   - Event Bus in-process (dispatch eventi di dominio via MediatR notifications)

✅ Done quando: l'API parte, espone `/health` e OpenAPI, un modulo demo si registra tramite `IModule`, unit e integration test girano in CI.

---

## Fase 2 — Frontend Skeleton (`apps/web`)

Obiettivo: app Vue 3 con architettura feature-first, pronta a ospitare feature/moduli.

1. **Setup**
   - Vite + Vue 3 + TypeScript strict, Tailwind CSS v4, Vue Router, Pinia, TanStack Query, VueUse, Motion, Heroicons
2. **Struttura**
   ```
   apps/web/src/
     app/        (bootstrap, provider, DI)
     core/       (api, auth, http, config, storage, errors, logger, permissions, interceptors)
     shared/     (components, composables, utils, types, constants, validators)
     features/   (una cartella per feature: components, pages, api, stores, composables, types, validators, routes)
     layouts/
     router/
     plugins/
     assets/
     types/
   ```
3. **Core services**
   - Http Client (wrapper Axios — mai Axios diretto nelle feature) + interceptor (auth, errori, retry)
   - Logger (console → predisposizione Sentry)
   - Error handling globale con la stessa gerarchia di errori del backend
   - Config/env tipizzata (Zod per validare `import.meta.env`)
   - Router con registrazione route per feature + guard (auth/permessi predisposti)
4. **Regole di stato**
   - Pinia solo per client state; TanStack Query per tutto il server state (documentare in `docs/frontend.md`)
5. **Testing**
   - Vitest + Vue Test Utils; Playwright configurato con un primo smoke test

✅ Done quando: `pnpm dev` mostra un layout con routing funzionante, una feature demo dimostra il pattern completo (page → composable → service → http client), test in CI.

---

## Fase 3 — Design System e Packages (`packages/`)

Obiettivo: libreria UI indipendente e pacchetti condivisi consumati da `apps/web`.

1. **`packages/types`** — tipi condivisi (contratti, enum, costanti)
2. **`packages/shared`** — utils, validators Zod, helper isomorfi
3. **`packages/ui`** — Design System
   - Design Tokens (CSS variables: colori semantici, spacing, radius, shadow, typography) — mai colori hardcoded
   - Theme Engine: light / dark / custom via token, nessuna modifica ai componenti
   - Componenti base, ognuno con `Component.vue`, `.types.ts`, `.test.ts`, `.stories.ts`, `index.ts`:
     - Button, Input, Card, Badge, Avatar → poi Dialog, Modal, Tooltip, Table
   - Storybook (o Histoire) come vetrina e documentazione
   - Test di accessibilità sui componenti (vitest-axe)
4. **`packages/sdk`** — SDK generato da OpenAPI
   - Script `scripts/generate-sdk` (es. `openapi-typescript` + client fetch/axios tipizzato)
   - Rigenerazione in CI quando cambia lo schema; il frontend consuma solo l'SDK

✅ Done quando: `apps/web` importa componenti da `@repo/ui` e chiama l'API tramite `@repo/sdk`, Storybook builda in CI, tema light/dark switchabile a runtime.

---

## Fase 4 — Modulo Auth (primo modulo completo, fa da template)

Obiettivo: il modulo Auth end-to-end definisce lo standard per tutti i moduli successivi.

1. **Struttura modulo** in `modules/auth/` con `README.md`, `module.json`, `frontend/`, `backend/`, `shared/`, `tests/`
2. **Backend**
   - Register, Login, Logout, Refresh Token (rotazione), password hashing
   - JWT con claims (userId, ruoli, permessi, tenantId predisposto)
   - Rate limiting su endpoint sensibili, audit degli accessi
3. **Frontend**
   - Pagine login/register, store sessione (Pinia), guard router, refresh automatico del token negli interceptor
4. **Testing completo** (unit, integration, e2e login flow)
5. **Documentare il "Module Contract"** in `docs/modules.md`: come si crea, installa, disabilita un modulo

✅ Done quando: login end-to-end funziona con refresh token, il modulo è disattivabile da `module.json`, la doc spiega come replicare il pattern.

---

## Fase 5 — Core Modules

Obiettivo: i moduli fondanti che ogni progetto userà. Ordine consigliato (per dipendenza):

1. **Users** — CRUD utenti, profilo (dipende da Auth)
2. **Roles + Permissions** — RBAC + PBAC, direttiva `v-can` / composable `usePermissions()`, enforcement backend via policy
3. **Settings** — impostazioni applicazione e utente, tipizzate
4. **Audit** — log delle azioni, interceptor automatico su comandi
5. **Notifications** — persistenza + centro notifiche (il canale realtime arriva in Fase 6)
6. **File Storage** — upload/download, provider astratto (locale → S3-compatibile)
7. **Localization** — i18n frontend (vue-i18n) + risorse backend
8. **Email** — template, provider astratto (SMTP → servizi esterni), invio in background

✅ Done quando: ogni modulo rispetta il Module Contract, ha README, test e può essere abilitato/disabilitato singolarmente.

---

## Fase 6 — Modulo Realtime (SignalR)

Obiettivo: infrastruttura realtime trasparente alle feature, come da specifica.

1. **Backend**
   - `NotificationHub`, `PresenceHub` (Chat/Dashboard hub quando serviranno)
   - Hub sottili: solo connessioni e routing; logica nei Service
   - Registro connessioni centralizzato (ConnectionId, UserId, TenantId, ruoli, gruppi, ultima attività) su Redis
   - Gruppi gestiti solo server-side; autenticazione JWT sugli Hub
   - Ponte Event Bus → SignalR: gli eventi di dominio (`EntityCreated`, `NotificationReceived`, …) vengono distribuiti ai client senza che i moduli conoscano SignalR
   - Predisposizione Redis Backplane per scale-out
2. **Frontend**
   - `RealtimeService` centralizzato (wrapper del client SignalR — mai usato direttamente dai componenti)
   - Composables: `useRealtime()`, `useNotifications()`, `usePresence()`, `useProgress()`
   - Auto-reconnect con retry esponenziale, heartbeat, ripristino gruppi, refresh token in riconnessione
   - Gestione offline: coda eventi pendenti, indicatore di stato, sync alla riconnessione
   - Integrazione TanStack Query: evento realtime → invalidation → UI aggiornata senza refresh
3. **Integrazione Notifications**: toast, badge, centro notifiche live

✅ Done quando: creando un'entità da un client, un secondo client vede la notifica e la tabella aggiornarsi senza refresh; la riconnessione ripristina gruppi e stato.

---

## Fase 7 — Enterprise Features

Obiettivo: capacità trasversali per progetti di grande scala. Da attivare in base alle esigenze:

- **Multi-Tenant** — risoluzione tenant (header/subdomain), query filter EF globali, isolamento nei gruppi SignalR
- **Feature Flags** — servizio flag con provider configurabile, composable `useFeature()`
- **CQRS completo** — separazione command/query già impostata con MediatR, eventuale read model dedicato
- **Background Jobs** — Hangfire o Quartz (email, report, cleanup)
- **Distributed Cache** — Redis come cache layer con invalidazione via eventi
- **Observability** — OpenTelemetry (traces, metrics), dashboard Grafana, Sentry frontend
- **Offline Support** — service worker, coda mutazioni

✅ Done quando: ogni feature è opt-in via configurazione e documentata.

---

## Fase 8 — Business Modules e Template App

Obiettivo: moduli applicativi e dimostrazione della composizione finale.

- **Dashboard** (widget system + realtime), **Calendar**, **Chat** (su ChatHub), **CMS**, **Reporting**, **Payments**, **AI Assistant**, **Workflow Engine**
- **App template di riferimento**: un'app demo che compone i moduli e serve da starter per i nuovi progetti (`scripts/create-project` per lo scaffolding)

### Stato: completata — con una riduzione di scope deliberata

Realizzati:

- **Dashboard** (`modules/dashboard`): widget system basato su
  `IDashboardWidgetProvider`, con contributi da Users, Notifications e Audit;
  griglia responsive, skeleton, refresh via realtime. È la landing page.
- **`scripts/create-project`**: copia+rinomina verificata (il progetto generato
  compila e passa i test). Vedi [docs/create-project.md](docs/create-project.md).
- `Skeleton` aggiunto al design system; enum serializzati per nome nell'API.

**Non realizzati, per scelta**: Calendar, Chat, CMS, Reporting, Payments, AI
Assistant, Workflow Engine. Costruirli qui significherebbe indovinare i
requisiti di prodotti che non esistono ancora, e ogni progetto futuro
erediterebbe quelle ipotesi insieme al costo di rimuoverle. L'obiettivo reale
della fase — _dimostrare la composizione finale_ — è soddisfatto dalla
Dashboard: mostra che un modulo nuovo si aggancia alla landing page
implementando un'interfaccia, senza che Dashboard o il frontend lo conoscano.
Un modulo di business si aggiunge quando un progetto lo richiede davvero.

---

## Trasversale a tutte le fasi

- **Documentazione**: aggiornare `docs/` man mano (`architecture.md`, `frontend.md`, `backend.md`, `coding-guidelines.md`, `api.md`, `security.md`, `deployment.md`, `design-system.md`, `conventions.md`, `branching.md`, `modules.md`). Ogni modulo ha il suo README.
- **Testing**: nessuna fase è completa senza unit + integration; e2e e accessibility test sui flussi principali.
- **CI**: la pipeline cresce con il progetto fino a coprire: install → lint → type-check → unit test → build → docker build → security scan → coverage.
- **Git Workflow**: `main` (produzione), `develop` (sviluppo), branch `feature/*`, `fix/*`, `release/*`, `hotfix/*`.

---

## Ordine di esecuzione e razionale

```
Fase 0  Monorepo + tooling + CI          (tutto il resto dipende da qui)
Fase 1  Backend skeleton                 (serve OpenAPI per l'SDK)
Fase 2  Frontend skeleton                (serve per consumare UI e SDK)
Fase 3  Design System + SDK              (sblocca sviluppo moduli full-stack)
Fase 4  Auth                             (template di modulo + prerequisito di quasi tutto)
Fase 5  Core Modules                     (Users → Roles/Permissions → Settings → Audit → Notifications → Files → i18n → Email)
Fase 6  Realtime                         (richiede Auth, Notifications, Redis)
Fase 7  Enterprise Features              (opt-in, su fondamenta stabili)
Fase 8  Business Modules + Template App  (composizione finale)
```

Il principio guida: **verticale prima che orizzontale** — arrivare presto a un flusso end-to-end funzionante (Fase 4) e usarlo come stampo, invece di costruire tutta l'infrastruttura in astratto.
