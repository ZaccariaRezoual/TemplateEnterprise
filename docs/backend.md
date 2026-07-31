# Backend Architecture

.NET 10 Minimal API following Clean Architecture. Solution layout under
`apps/api`:

```
src/
  Domain/               Entities, value objects, domain events. References NOTHING.
  Application/          Use cases (MediatR), validation, exceptions, abstractions.
                        References Domain only.
  Infrastructure/       EF Core (PostgreSQL), event bus implementation.
                        References Application.
  Modules.Abstractions/ The Module Contract (IModule, manifest, loader).
  Api/                  Composition root: middleware, ProblemDetails, health,
                        OpenAPI, module loading. References everything.
tests/
  UnitTests/            Pure logic tests (behaviors, module loader).
  IntegrationTests/     In-memory host tests (WebApplicationFactory).
```

## Dependency rule

Inner layers never know outer ones: Domain has zero references; Application
references Domain only; only the Api composition root references
Infrastructure. Entities are never exposed over HTTP — endpoints return
DTO/Contracts defined next to their use cases.

## Cross-cutting pipeline

Every HTTP request flows through, in order: exception handler (ProblemDetails)
→ correlation id → security headers → Serilog request logging → CORS → rate
limiter → endpoint. Every MediatR request flows through: request logging
behavior → validation behavior (FluentValidation) → handler.

Expected failures are expressed with the shared exception hierarchy
(`AppException` → `ValidationException` 400, `UnauthorizedException` 401,
`ForbiddenException` 403, `NotFoundException` 404, `BusinessException` 422);
anything else surfaces as an anonymous 500 with a `correlationId` for support.

## Events

Modules publish domain events (records implementing `IDomainEvent`) through
`IEventBus` without knowing subscribers. Subscribers implement
`INotificationHandler<DomainEventNotification<TEvent>>`. The current transport
is in-process MediatR (`MediatREventBus`); it will be bridged to SignalR in
Fase 6 without touching publishers.

## Observability

Serilog writes to console and Seq (http://localhost:5341). Every log line
carries the request `CorrelationId` (header `X-Correlation-ID`, generated when
absent). Health endpoints: `/health/live` (process up), `/health/ready`
(PostgreSQL + Redis reachable), `/health` (everything).

## Running locally

```bash
docker compose -f docker/docker-compose.yml up -d   # Postgres, Redis, Seq
dotnet run --project apps/api/src/Api                # http://localhost:5080
```

OpenAPI document: `GET /openapi/v1.json` (input for the SDK generator, Fase 3).
