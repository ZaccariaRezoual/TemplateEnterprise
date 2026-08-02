# The Module Contract

A module is a self-contained feature (Auth, Users, Notifications, ...) living
under `modules/<name>/` that can be installed, disabled, updated and configured
without touching any other module.

## Anatomy

```
modules/<name>/
  README.md       What the module does, endpoints, configuration
  module.json     Manifest: name, version, dependencies, enabled
  frontend/       Vue feature packages (from Fase 4)
  backend/        .NET class library implementing IModule
  shared/         Contracts shared between the two sides
  tests/          Module-specific tests
```

## Backend rules

1. The backend project references `EnterpriseFramework.Application` and
   `EnterpriseFramework.Modules.Abstractions` — never `Infrastructure`, never
   another module's project (modules talk via events).
2. `module.json` is embedded in the assembly (`LogicalName="module.json"`);
   `IModule.Name` must match its `name` field.
3. `IModule.ConfigureServices` registers the module's MediatR handlers and
   validators; `IModule.MapEndpoints` maps routes under `/api/<module>`.
4. The host lists the module assembly in `Program.cs`; `ModuleLoader` reads
   manifests, applies the `Modules:<Name>:Enabled` configuration override,
   validates dependencies and initializes modules in topological order.
5. Cross-module communication happens ONLY through domain events published on
   `IEventBus` — a module never calls another module's services directly.
   Events crossing a boundary are **public contracts** and live in the
   publisher's `shared/` project (e.g. `EnterpriseFramework.Modules.Auth.Contracts`),
   so a subscriber references the event shapes, never the implementation.
   Changing one is a breaking change: add fields, never remove or repurpose.
   When a module needs behavior rather than notification, the HOST defines an
   extension point in `Application/Abstractions` and both sides depend on that
   (see `IUserClaimsEnricher`: Authorization contributes claims to the tokens
   Auth issues, with neither module referencing the other, and
   `IDashboardWidgetProvider`: any module contributes a tile to the landing
   page without Dashboard knowing it exists).
6. **Persistence is module-owned**: a module with entities defines its own
   `DbContext` in its own PostgreSQL schema, with its own migration history.
   Installing/removing the module never touches other modules' data. A module
   needing another's data keeps a **projection** built from that module's
   events (see `modules/users`), and references foreign entities by id — never
   by a foreign key into another schema.
   Migrations: `dotnet ef migrations add <Name> --project modules/<name>/backend
--startup-project apps/api/src/Api`. Startup migration and seeding are gated
   by `Modules:AutoMigrate` (on in development only); production runs them as
   an explicit release step, so no instance boot mutates a live database.
7. **Endpoints are authenticated by default**: the host's fallback policy
   requires a signed-in caller. Anonymous endpoints opt out explicitly with
   `AllowAnonymous()` — and should say why in a comment. Endpoints needing
   more than authentication declare `RequirePermission("resource.action")`.
8. Module settings live under `Modules:<Name>:*` in configuration.

## Frontend rules

A module with UI ships it as a workspace package in `modules/<name>/frontend`
(`@enterprise/module-<name>`), included by the `modules/*/frontend` workspace
pattern. The host consumes at most three exports:

1. `install<Name>Module(host)` called at bootstrap — the module plugs into
   seams CORE exposes (e.g. the auth token provider); core never imports a
   module.
2. `<name>Routes` composed in the app's route registry.
3. Optional stores/composables for chrome-level UI (e.g. the session in the
   header).

Module frontends depend on `@enterprise/sdk`, `@enterprise/shared` and
`@enterprise/ui` — never on `apps/web` internals, never on another module.

## Disabling a module

```json
{ "Modules": { "Demo": { "Enabled": false } } }
```

Its services are not registered and its endpoints are not mapped. If an enabled
module depends on it, startup fails fast with a clear error.

## Reference implementations

- [`modules/auth`](../modules/auth/README.md) — **the canonical template**:
  module-owned persistence with migrations, typed options, public event
  contracts, module-scoped rate limiting, frontend package with
  store/guard/pages, and tests at every level.
- [`modules/authorization`](../modules/authorization/README.md) — plugging
  into a host extension point (`IUserClaimsEnricher`), permission-based
  endpoint policies, a frontend directive and guard.
- [`modules/users`](../modules/users/README.md) — an event-driven projection
  of another module's data, and declared module dependencies.
- [`modules/audit`](../modules/audit/README.md) — observing the whole system
  from the outside, via a pipeline behavior and event subscribers.
- [`modules/settings`](../modules/settings/README.md) — typed, layered
  configuration other modules read through one service.
- [`modules/email`](../modules/email/README.md) — a swappable transport, an
  outbox instead of inline sending, and a module with no endpoints at all.
- [`modules/storage`](../modules/storage/README.md) — a provider abstraction
  that deliberately hides paths, and the security rules around uploads.
- [`modules/notifications`](../modules/notifications/README.md) — a public
  contract other modules publish to, ready for realtime push in Fase 6.
- [`modules/localization`](../modules/localization/README.md) — resources over
  database rows, served to both the API and vue-i18n.
- [`modules/realtime`](../modules/realtime/README.md) — a module that makes
  every other module live without any of them referencing it, via a marker
  interface and an open discovery of its implementations.
- [`modules/site`](../modules/site/README.md) — a frontend-only module so far:
  the public site, whose content is a typed file the project owns and whose
  pages a project may replace outright.
- [`modules/demo`](../modules/demo/README.md) — the minimal skeleton: query,
  validated command, event publish/subscribe.
