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
  assets/     Styles and design tokens (move to packages/ui in Fase 3)
  types/      Ambient declarations (route meta, Vite env)
```

## The request path

```
Component → Composable (TanStack Query) → Feature service (api/) → HttpClient → Axios
```

Each arrow is a rule:

- Components never fetch. They call a composable.
- Composables never know URLs. They call the feature service.
- Feature services never know the transport. They call `httpClient`.
- **Nothing outside `core/http` imports Axios.** From Fase 3, feature services
  delegate to the generated SDK, and only those files change.

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

Three token layers — primitives → semantic → component — defined in
`assets/styles`. Components use **semantic tokens only** (`bg-surface`,
`text-danger`); a literal color anywhere outside the token files is a bug. The
theme engine (`useTheme`) switches themes by setting `data-theme` on `<html>`;
no component is theme-aware. Full rules in the CLAUDE.md "Design System"
section; the token catalog moves to `packages/ui` in Fase 3.

## Configuration

Every environment variable is declared and validated with Zod in
`core/config/env.ts`. A missing or malformed value throws at startup instead of
surfacing as `undefined` inside a feature. Import `env`, never
`import.meta.env`.

| Variable              | Default | Purpose                        |
| --------------------- | ------- | ------------------------------ |
| `VITE_API_BASE_URL`   | `/api`  | API base URL (proxied)         |
| `VITE_API_TIMEOUT_MS` | `15000` | Request timeout                |
| `VITE_LOG_LEVEL`      | `info`  | Minimum level the logger emits |

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
