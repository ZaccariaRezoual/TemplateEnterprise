# @enterprise/sdk

Typed client for the API, **generated** from its OpenAPI document.

## The rule

The frontend talks to the API exclusively through this package. A feature that
builds a URL by hand bypasses the contract: when the endpoint changes, nothing
fails until runtime. Going through the SDK turns that into a type error at
build time.

## How generation works

```
apps/api  ──build──▶  apps/api/openapi/v1.json  ──generate──▶  src/generated/schema.ts
```

`dotnet build` writes the OpenAPI document on every build (no need to run the
API), so the checked-in document always matches the code and its diff is
reviewable in pull requests.

Regenerate the types after changing an endpoint or a contract:

```bash
dotnet build apps/api/src/Api          # refresh the OpenAPI document
pnpm --filter @enterprise/sdk generate # regenerate the typed schema
```

CI fails if the committed output is stale, so the SDK can never silently drift
from the API.

## Files

| Path                      | Owner     | Notes                                    |
| ------------------------- | --------- | ---------------------------------------- |
| `src/generated/schema.ts` | generator | **Never edit by hand** — overwritten     |
| `src/client.ts`           | humans    | Client factory and framework integration |
| `src/index.ts`            | humans    | Public surface of the package            |

## Usage

The application creates one client at startup and injects the transport, so
every request keeps the framework's interceptors, correlation ids and error
mapping:

```ts
import { createApiClient } from "@enterprise/sdk";

const api = createApiClient({ baseUrl: "/api", fetch: instrumentedFetch });
const { data } = await api.GET("/api/demo/ping");
```
