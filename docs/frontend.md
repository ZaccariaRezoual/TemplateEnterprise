# Frontend Architecture

Vue 3 + TypeScript (strict) application under `apps/web`, organized
feature-first.

```
src/
  app/        Composition root: App shell, providers (query client)
  core/       Things that exist exactly once: config, http, errors, logger,
              storage. Features depend on core; core never depends on features.
  shared/     Reusable across features: components, composables, utils
  features/   One folder per feature, self-contained:
              components/ pages/ api/ stores/ composables/ types/
              validators/ routes.ts
  layouts/    Page shells (default, blank) selected via route meta
  router/     Route registry composing the features' routes
  assets/     Stylesheet entry importing the design system's tokens
  types/      Ambient declarations (route meta, Vite env)
```

## Two areas

Routes split by `meta.requiresAuth`, in `router/adminArea.ts`:

- routes that require a session are rebased under **`/admin`** and registered
  by `registerAdminArea`;
- everything else stays at the root, where the public site lives
  (`modules/site`, wrapped in its own shell via `meta.publicSite`).

`registerAdminArea` **requires the proof returned by `installAuthModule`**.
`meta.requiresAuth` is enforced by that module's guard and by nothing else, so
an application assembled without it would otherwise carry an administrative
shell that opens to anyone. Demanding the token makes that assembly fail to
compile instead of failing silently.

The router is installed on the app **after** the modules, because
`app.use(router)` starts the first navigation and a deep link into the private
area must find it already registered.

## The request path

```
Component → Composable (TanStack Query) → Feature service (api/) → SDK → API
```

Each arrow is a rule:

- Components never fetch. They call a composable.
- Composables never know operations. They call the feature service.
- Feature services call the generated SDK — **never a hand-written URL**. Paths
  are checked against the OpenAPI document, so a renamed endpoint is a compile
  error, not a production incident.
- Cross-cutting concerns (correlation id, auth token from Fase 4, error
  mapping) live only in `core/api/apiClient.ts`, applied to every call by
  construction.

`request()` in `core/api/apiClient.ts` bridges the two error conventions: the
SDK reports failures as a value (`{ error }`), while TanStack Query and
`try`/`catch` expect a rejection. It is the single place that classifies API
failures.

## State: two homes, no overlap

| State                    | Home           | Example                        |
| ------------------------ | -------------- | ------------------------------ |
| Anything the server owns | TanStack Query | user list, current entity      |
| Client-only UI state     | Pinia          | sidebar open, theme preference |

Never copy server data into a Pinia store: it immediately becomes a second
source of truth that nothing invalidates. Query keys live next to the
composables that use them (`demoKeys`) so cache invalidation — including the
invalidation SignalR will trigger in Fase 6 — never uses a hand-typed string.

## Errors

`core/http/errorMapper.ts` is the inverse of the backend's
`GlobalExceptionHandler`: it turns every failed response into a typed error
(`ValidationError`, `UnauthorizedError`, `ForbiddenError`, `NotFoundError`,
`BusinessError`, `ServerError`, `NetworkError`). UI code branches on the error
type, never on a status code, and validation errors carry per-field messages
ready to display next to inputs.

## Styling and theming

Both come from the design system, `@enterprise/ui`: the app imports
`@enterprise/ui/tokens.css` and uses **semantic tokens only** (`bg-surface`,
`text-danger`). A literal color in application code is a bug. `installTheme()`
is called once in `main.ts`; no component is theme-aware. See
[design-system.md](design-system.md).

## Configuration

Every environment variable is declared and validated with Zod in
`core/config/env.ts`. A missing or malformed value throws at startup instead of
surfacing as `undefined` inside a feature. Import `env`, never
`import.meta.env`.

| Variable              | Default | Purpose                                           |
| --------------------- | ------- | ------------------------------------------------- |
| `VITE_API_BASE_URL`   | `""`    | API **origin**, empty for same-origin (see below) |
| `VITE_API_TIMEOUT_MS` | `15000` | Request timeout                                   |
| `VITE_LOG_LEVEL`      | `info`  | Minimum level the logger emits                    |

`VITE_API_BASE_URL` is an origin, not a path prefix: the SDK's paths already
contain the full route (`/api/demo/ping`). Setting it to `/api` would produce
`/api/api/…` on every call. Use an absolute origin only when the API is served
from another host.

## Running and testing

```bash
pnpm --filter @enterprise/web dev        # http://localhost:5173, proxies /api → :5080
pnpm --filter @enterprise/web test       # Vitest unit + component tests
pnpm --filter @enterprise/web test:e2e   # Playwright (requires the API running)
pnpm --filter @enterprise/web build      # type-check + production bundle
```

The dev server proxies `/api` to `http://localhost:5080`, so the browser always
makes same-origin calls: no CORS in development, and the deployed topology
(one reverse proxy in front of both) behaves identically.
