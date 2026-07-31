# Enterprise Features

Capabilities a large project eventually needs, built into the foundation but
**off by default**. Each is opt-in through configuration, so a small project
carries none of their cost and a growing one turns them on without a rewrite.

They live in the host and in `Application/Abstractions` rather than in
modules: every module may depend on them, and none should have to be installed
for the application to work.

---

## Multi-Tenant

Hard to retrofit, which is why it ships now even though it is off.

```json
{
  "Tenancy": {
    "Enabled": true,
    "Strategy": "Header",
    "HeaderName": "X-Tenant-Id",
    "TenantlessPaths": ["/health", "/openapi", "/api/auth", "/api/localization"]
  }
}
```

Strategies: `Header`, `Subdomain` (first label of the host), `Claim` (the
`tenant` claim of the access token).

**How isolation is enforced.** An entity implements `ITenantOwned`; the
module calls `modelBuilder.ApplyTenantFilters(_tenantContext)` once in
`OnModelCreating`. Every query in that context is then tenant-scoped. This
matters because manual filtering fails _silently_: one forgotten `Where` leaks
another customer's data with no error anywhere. `modules/users` is the
worked example.

**The security-critical part** is in the resolution middleware: a
client-supplied tenant is checked against the tenant claim of the caller's
token, and a mismatch is a 403. Without that check multi-tenancy is a
suggestion — anyone could read another customer's data by editing a header.

Other decisions worth knowing:

- The tenant is **write-once per request**. A service able to switch tenants
  mid-request is the shape every cross-tenant leak takes.
- Requests to non-tenantless paths without a tenant are **rejected**, not
  allowed through unscoped.
- SignalR role groups are **tenant-scoped** when tenancy is on. Otherwise an
  event sent to `role:Administrator` would reach the administrators of every
  customer — the one broadcast that crosses the boundary query filters protect
  everywhere else.
- Use `IgnoreQueryFilters()` deliberately and rarely: the Users projection
  needs it so an idempotency check sees rows of any tenant instead of
  violating the primary key.

When disabled, `ITenantContext.TenantId` is null, filters are inert, and the
middleware does nothing — the same code runs in both kinds of deployment.

---

## Feature Flags

```json
{
  "Features": {
    "Flags": {
      "beta.newDashboard": {
        "Enabled": false,
        "EnabledForUsers": ["11111111-1111-1111-1111-111111111111"],
        "EnabledForTenants": []
      }
    }
  }
}
```

Backend: inject `IFeatureFlags`. Frontend: `useFeatures()` or `v-feature`.

```vue
<NewDashboard v-feature="'beta.newDashboard'" />
```

**A flag is not a permission.** Flags decide what this _deployment_ ships;
permissions decide what this _user_ may do and are enforced by the API. Using
a flag to hide something sensitive would put a security decision in a file
anyone can edit. That is also why the API serves flags anonymously — they are
switches, not secrets.

Allow-lists only: there is no "on for everyone except…", because that turns a
rollout switch into an authorization rule. An unknown flag is off, so a typo
hides a feature rather than revealing an unfinished one, and deleting a flag
does not resurrect the code path it guarded.

Configuration-backed by default, so a flag change is a reviewable, revertible
deployment artifact. Swap the provider for runtime toggles; callers do not
change.

---

## Distributed Cache

Redis when `ConnectionStrings:Redis` is set, process memory otherwise — no
configuration needed to switch.

```csharp
var report = await cache.GetOrCreateAsync(
    $"report:{tenantId}:{month}",
    ct => BuildReportAsync(ct),
    TimeSpan.FromMinutes(10),
    cancellationToken);
```

Include everything the result depends on in the key — tenant, user, filters —
or one caller's result is served to another.

Cache failures are **swallowed**: a cache exists to make a working system
faster, so an unreachable Redis degrades to recomputing rather than turning
every request into an error. The exception is invalidation, which is logged at
error level, because silently serving invalidated data is worse than being
slow.

The in-memory fallback is **per-replica**: an invalidation on one instance
leaves the others stale. Fine in development, wrong behind a load balancer.

---

## Observability

```json
{
  "Observability": {
    "Enabled": true,
    "ServiceName": "enterprise-api",
    "OtlpEndpoint": "http://localhost:4317",
    "SamplingRatio": 0.1
  }
}
```

OpenTelemetry traces and metrics over OTLP, to any compatible backend (Grafana
Alloy/Tempo, Jaeger, Honeycomb). Complements Serilog rather than replacing it:
logs say _what_ happened, traces say _where the time went_ across services, and
both carry the same correlation id so one identifier moves between them.

Health probes are excluded from tracing — they fire constantly and would bury
real traffic. Sampling is full by default and should be lowered under load.

Off by default because an exporter that cannot reach its collector adds
latency and noise to every request.

---

## Deliberately not built

**Offline support** (service worker, mutation queue) — genuinely speculative.
Its design depends entirely on which operations a product must support
offline and how conflicts resolve, and a wrong guess baked into a template
costs more than its absence.

**A dedicated read model / full CQRS** — the command/query split already
exists through MediatR. A separate read store is a response to a measured
problem, and adding one preemptively doubles the write path for every feature.

**A durable background-job system** (Hangfire/Quartz) — the Email module's
outbox shows the pattern, and the choice of engine and storage belongs to the
project. `IEmailOutbox` is the seam to swap.
