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

## Disabling a module

```json
{ "Modules": { "Demo": { "Enabled": false } } }
```

Its services are not registered and its endpoints are not mapped. If an enabled
module depends on it, startup fails fast with a clear error.

## Reference implementation

See [`modules/demo`](../modules/demo/README.md): minimal but complete example
of query, validated command and event publish/subscribe. Copy it as the
starting point for new modules until the Auth module (Fase 4) becomes the
canonical template.
