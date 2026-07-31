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
packages/      Shared libraries (ui, sdk, shared, types)
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
```

Seq (structured log viewer) is available at http://localhost:5341 once compose is up.

## Development workflow

- Branches: `main` (production), `develop` (integration), `feature/*`, `fix/*`, `release/*`, `hotfix/*`.
- Commits follow [Conventional Commits](https://www.conventionalcommits.org) (enforced by Commitlint via Husky).
- Every PR runs the CI pipeline: install → lint → type-check → test → build.
