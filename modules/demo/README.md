# Demo Module

Reference implementation of the Module Contract, used to prove the module
system end-to-end. It will be replaced by real modules (Auth, Users, ...) from
Fase 4 onward — copy its structure when creating a new module.

What it demonstrates:

- `IModule` implementation discovered and loaded by the host via `module.json`
- A MediatR **query** (`GET /api/demo/ping`)
- A MediatR **command with FluentValidation** (`POST /api/demo/echo`)
- Publishing a **domain event** through `IEventBus` and handling it in-process

## Endpoints

| Method | Route            | Description                                        |
| ------ | ---------------- | -------------------------------------------------- |
| GET    | `/api/demo/ping` | Liveness-style sample query                        |
| POST   | `/api/demo/echo` | Validated command that publishes `DemoEchoedEvent` |

## Configuration

Disable the module without touching code, from `appsettings.json`:

```json
{ "Modules": { "Demo": { "Enabled": false } } }
```
