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
6. **Persistence is module-owned**: a module with entities defines its own
   `DbContext` in its own PostgreSQL schema, with its own migration history
   (`.editorconfig` in the Migrations folder marks them as generated code).
   Installing/removing the module never touches other modules' data.
   Migrations: `dotnet ef migrations add <Name> --project modules/<name>/backend
--startup-project apps/api/src/Api`.
7. **Endpoints are authenticated by default**: the host's fallback policy
   requires a signed-in caller. Anonymous endpoints opt out explicitly with
   `AllowAnonymous()` — and should say why in a comment.
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
  module-owned persistence with migrations, typed options, domain events,
  module-scoped rate limiting, frontend package with store/guard/pages, and
  tests at every level.
- [`modules/demo`](../modules/demo/README.md) — the minimal skeleton: query,
  validated command, event publish/subscribe.
