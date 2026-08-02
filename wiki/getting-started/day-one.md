# Primo giorno

Da zero all'applicazione in esecuzione. Se qualcosa non torna, la sezione
[Quando non parte](#quando-non-parte) copre i casi che capitano davvero.

## Cosa serve installato

| Strumento | Versione                      | Nota                                             |
| --------- | ----------------------------- | ------------------------------------------------ |
| Node.js   | `^22.18.0` oppure `>=24.11.0` | Vincolo reale delle dipendenze, non un capriccio |
| pnpm      | >= 9                          | `corepack enable`                                |
| .NET SDK  | 10                            |                                                  |
| Docker    | in esecuzione                 | Postgres, Redis, Seq e gli integration test      |

Verifica in un colpo solo:

```bash
node -v && pnpm -v && dotnet --version && docker ps
```

## Avviare tutto

```bash
# 1. dipendenze
pnpm install
dotnet build

# 2. infrastruttura locale (PostgreSQL, Redis, Seq)
docker compose -f docker/docker-compose.yml up -d

# 3. API → http://localhost:5080
dotnet run --project apps/api/src/Api

# 4. in un ALTRO terminale, il frontend → http://localhost:5173
pnpm --filter @enterprise/web dev
```

In Development le migrations dei moduli e il seeding partono da soli: il
database si popola al primo avvio, senza comandi aggiuntivi.

## Entrare

**admin@example.com** / **Password123!**

È il bootstrap administrator, seedato **solo in Development**. Serve perché una
registrazione normale crea un account `BasicUser`, e promuoverlo richiede un
permesso che nessuno ha ancora. Da qui puoi amministrare utenti e ruoli.

La password è pubblica in questo repository: un deployment che abilita il seed
**deve** impostarne una propria (`Modules:Auth:BootstrapAdmin`).

## Dove guardare

| Cosa              | Dove                                                   |
| ----------------- | ------------------------------------------------------ |
| Applicazione      | http://localhost:5173                                  |
| API               | http://localhost:5080                                  |
| Salute dell'API   | http://localhost:5080/health/ready                     |
| Contratto OpenAPI | http://localhost:5080/openapi/v1.json                  |
| Log strutturati   | http://localhost:5341 (Seq — `admin` / `dev_password`) |
| Design system     | `pnpm --filter @enterprise/ui storybook` → :6006       |

Seq merita un'apertura il primo giorno: è lì che si vede il correlation id
legare insieme richiesta, comando ed eventi di dominio.

## Eseguire i test

```bash
dotnet test                              # backend (unit + integration, serve Docker)
pnpm test                                # frontend, tutti i package
pnpm --filter @enterprise/web test:e2e   # e2e (serve l'API attiva)
pnpm typecheck && pnpm lint
```

Se sono verdi, l'ambiente è a posto.

## Quando non parte

**`dotnet build` chiede un database.** La build avvia il host per generare il
documento OpenAPI, e in Development le migrations partono all'avvio: serve
Postgres su (`docker compose … up -d`), oppure `Modules:AutoMigrate=false`.

**«The process cannot access the file … .dll».** Hai l'API in esecuzione: tiene
i lock sulle dll. Fermala prima di ricompilare. Il messaggio non lo dice.

**`pnpm install` rifiuta la versione di Node.** Il vincolo è reale (`vue-router`
5): aggiorna Node, non forzare l'installazione.

**Il frontend parte ma ogni chiamata dà 500 o non risponde.** L'API non è
attiva: il dev server fa da proxy su `/api`, non serve i dati.

**Gli integration test falliscono all'avvio.** Docker non è in esecuzione: usano
Testcontainers, quindi un Postgres vero.

**429 mentre lavori.** Il rate limiter è per IP; in Development i limiti sono
già alzati in `appsettings.Development.json`. Se capita, di solito sono e2e
lanciati in parallelo.

## E adesso

1. Leggi [il giro del repository](tour.md) — 15 minuti, ti evita di cercare
   le cose nei posti sbagliati.
2. Quando devi fare qualcosa, parti da [pattern](../patterns/README.md).
3. Tieni [il glossario](glossary.md) aperto le prime settimane: metà delle
   parole di questo repo hanno un significato preciso.
