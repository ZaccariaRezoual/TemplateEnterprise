# Enterprise Vue.NET Framework 2026

Enterprise-grade monorepo template used as the foundation for all company projects (SaaS, CRM, ERP, dashboards, B2B/B2C portals, marketplaces). New products are built by composing independent, reusable modules on top of this framework.

- **Vision & architecture guidelines**: [Struttura.md](Struttura.md)
- **Build plan (phases 0–8)**: [PLAN.md](PLAN.md)
- **Working agreement for AI-assisted development**: [CLAUDE.md](CLAUDE.md)

## Stack

| Layer    | Technology                                                                 |
| -------- | -------------------------------------------------------------------------- |
| Frontend | Vue 3, TypeScript, Vite, Tailwind CSS v4, Pinia, TanStack Query, Motion    |
| Backend  | .NET 10, ASP.NET Core Minimal API, EF Core, PostgreSQL, MediatR, Serilog   |
| Realtime | ASP.NET Core SignalR (Redis backplane ready)                               |
| DevOps   | pnpm workspaces, Docker Compose, GitHub Actions, Husky, Commitlint, CodeQL |

## Repository layout

```
apps/          Deployable applications (api, web)
packages/      Shared libraries
  ui/            Design system: tokens, theme engine, components (+ Storybook)
  sdk/           Typed API client, generated from the OpenAPI document
  shared/        Framework-agnostic utilities and validation schemas
  types/         Types and enums shared across the monorepo
modules/       Independent feature modules (auth, users, ...)
docs/          Architecture and process documentation
docker/        Local infrastructure (PostgreSQL, Redis, Seq)
scripts/       Automation scripts (SDK generation, scaffolding)
tools/         Shared tooling configuration
```

## Getting started

Prerequisites: Node.js >= 22, pnpm >= 9 (via `corepack enable`), .NET 10 SDK, Docker.

```bash
# Install JS dependencies (whole workspace)
pnpm install

# Build the .NET solution
dotnet build

# Start local infrastructure (PostgreSQL, Redis, Seq)
docker compose -f docker/docker-compose.yml up -d

# Run the API (http://localhost:5080)
dotnet run --project apps/api/src/Api

# Run the web app (http://localhost:5173, proxies /api to the API)
pnpm --filter @enterprise/web dev

# Browse the design system
pnpm --filter @enterprise/ui storybook

# Run tests
dotnet test                              # backend
pnpm test                                # frontend unit + component
pnpm --filter @enterprise/web test:e2e   # end-to-end (API must be running)

# Regenerate the typed SDK after changing an API contract
node scripts/generate-sdk.mjs
```

Sign in at http://localhost:5173 with **admin@example.com** / **Password123!** —
the bootstrap administrator seeded in Development only. Registration creates
plain accounts (`BasicUser`), so this is the account that can administer users
and roles. Configure or disable it under `Modules:Auth:BootstrapAdmin`; a
deployment that enables it **must** set its own password, since this one is
public in the repository. See [modules/auth/README.md](modules/auth/README.md).

Seq (structured log viewer) is available at http://localhost:5341 once compose
is up (login `admin` / `dev_password`). API health: `/health/ready`. OpenAPI
document: `/openapi/v1.json`.

## Documentation

- [docs/backend.md](docs/backend.md) — Clean Architecture layers, pipeline, events
- [docs/frontend.md](docs/frontend.md) — feature-first structure, state rules, theming
- [docs/design-system.md](docs/design-system.md) — tokens, theming, component conventions
- [docs/modules.md](docs/modules.md) — the Module Contract
- [packages/sdk/README.md](packages/sdk/README.md) — how the SDK is generated and kept in sync

## Development workflow

- Branches: `main` (production), `develop` (integration), `feature/*`, `fix/*`, `release/*`, `hotfix/*`.
- Commits follow [Conventional Commits](https://www.conventionalcommits.org) (enforced by Commitlint via Husky).
- Every PR runs the CI pipeline: install → lint → type-check → test → build.
