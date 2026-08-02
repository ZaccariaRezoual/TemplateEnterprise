# Giro del repository

Quindici minuti per sapere dove stanno le cose. Non serve capire tutto: serve
non cercarle nel posto sbagliato.

## La mappa

```
apps/
  api/          backend .NET — il host, non la logica di dominio
  web/          applicazione Vue — il guscio, non le funzionalità
packages/
  ui/           design system (Storybook)
  sdk/          client tipizzato GENERATO da OpenAPI
  shared/       utility e validator isomorfi
  types/        tipi condivisi
modules/        ⟵ QUI vive quasi tutto
docs/           regole normative
wiki/           come si fa (sei qui)
plans/          piani di lavoro
scripts/        generate-sdk, create-project
docker/         Postgres, Redis, Seq
```

## La cosa da capire per prima

> **`apps/` è quasi vuoto di funzionalità. Le funzionalità stanno in
> `modules/`.**

`apps/api` è il composition root: registra i moduli, la sicurezza, la pipeline.
`apps/web` è il guscio: layout, router, bootstrap. Auth, utenti, permessi,
notifiche, dashboard — tutto in `modules/`.

Il criterio quando aggiungi qualcosa: **servirebbe ad almeno due progetti?**
Sì → `modules/` o `packages/`. No → `apps/`.

## Un modulo dentro

```
modules/auth/
  README.md      decisioni e contratti — leggilo prima del codice
  module.json    nome, versione, dipendenze, enabled
  backend/       <Nome>Module.cs, Features/, Domain/, Persistence/
  shared/        contratti PUBBLICI (eventi, DTO) che altri moduli referenziano
  frontend/      package @enterprise/module-<nome>
  tests/
```

`modules/auth` è il **template canonico**: persistenza propria, opzioni
tipizzate, eventi pubblici, rate limiting di modulo, frontend agganciato ai
seam del core. Quando non sai come si fa una cosa in un modulo, guarda lì.

## Il backend in una schermata

```
apps/api/src/
  Api/                composition root: endpoint, sicurezza, error handling
  Application/        casi d'uso, astrazioni (Abstractions/ = i seam)
  Domain/             entità, value object, eventi
  Infrastructure/     EF Core, Redis, implementazioni
```

**Dependency rule**: i livelli interni non conoscono quelli esterni. Domain non
referenzia EF Core, Application non conosce PostgreSQL.

`Application/Abstractions/` merita un'occhiata: contiene i punti di estensione
(`IUserClaimsEnricher`, `IRealtimeEvent`, `IDashboardWidgetProvider`) che
permettono ai moduli di collaborare senza conoscersi. È l'idea centrale del
framework.

## Il frontend in una schermata

```
apps/web/src/
  app/        bootstrap, provider
  core/       ciò che esiste una volta sola: http, config, logger, storage
  shared/     riusabile fra feature
  features/   una cartella per funzionalità, autosufficiente
  layouts/    gusci di pagina
  router/     compone le route delle feature e dei moduli
```

Il percorso di ogni richiesta, sempre lo stesso:

```
Componente → Composable → Feature service (api/) → SDK → API
```

E la divisione dello stato: **TanStack Query** per ciò che viene dal server,
**Pinia** per ciò che vive solo nel browser. Mai mescolare.

## Cosa leggere, in ordine

1. Questo giro.
2. [`docs/modules.md`](../../docs/modules.md) — il Module Contract, il
   documento più importante del repository.
3. `modules/auth/README.md` — il pattern applicato per intero.
4. [`docs/backend.md`](../../docs/backend.md) e
   [`docs/frontend.md`](../../docs/frontend.md).
5. [`docs/design-system.md`](../../docs/design-system.md) prima di scrivere
   markup.

`Struttura.md` e `PLAN.md` servono per il quadro d'insieme e per sapere cosa
esiste; non sono letture da primo giorno.

## Il modo più veloce per orientarsi davvero

Segui **un dato dall'inizio alla fine**. Per esempio la registrazione:

1. `modules/auth/backend/Features/Register/` — il comando e l'handler
2. `modules/auth/shared/Events/UserRegistered.cs` — il contratto pubblico
3. `modules/users/backend/Features/Projection/` — chi lo ascolta e costruisce
   la propria proiezione
4. `modules/authorization/backend/Features/GrantDefaultRole/` — chi assegna il
   ruolo

Quattro file in tre moduli che non si conoscono fra loro. Se questo passaggio
ti è chiaro, il resto del repository si legge da solo.
