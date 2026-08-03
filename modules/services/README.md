# Services Module

The catalogue of what the organization offers: administered under
`/admin/services`, published on the showcase at `/services`.

It owns **both halves**. Whoever owns the data owns its representation — the
Site module stays the shell and the institutional pages (home, about,
contact), and this module brings its own public pages with it.

## Two APIs that never meet

| Audience     | Routes                                          | Sees                         |
| ------------ | ----------------------------------------------- | ---------------------------- |
| The public   | `GET /api/services`, `GET /api/services/{slug}` | published, non-archived only |
| The admin UI | `/api/admin/services/*`                         | everything                   |

Two route families, not one route with a `?includeDrafts=true` flag. A flag is
something a caller can set and a reviewer can miss, and **a draft on the
public site is the one failure this module exists to prevent**. The filter
lives in a single expression — `PublicCatalogue.Visible` — so there is one
place to get it right instead of two.

| Method | Route                                             | Permission        |
| ------ | ------------------------------------------------- | ----------------- |
| GET    | `/api/services`                                   | anonymous         |
| GET    | `/api/services/{slug}`                            | anonymous         |
| GET    | `/api/admin/services`                             | `services.read`   |
| GET    | `/api/admin/services/{id}`                        | `services.read`   |
| POST   | `/api/admin/services`                             | `services.write`  |
| PUT    | `/api/admin/services/{id}`                        | `services.write`  |
| POST   | `/api/admin/services/{id}/images`                 | `services.write`  |
| PUT    | `/api/admin/services/{id}/images/{imageId}/cover` | `services.write`  |
| DELETE | `/api/admin/services/{id}/images/{imageId}`       | `services.write`  |
| POST   | `/api/admin/services/{id}/archive`                | `services.delete` |

A draft requested by slug answers **404**, exactly like an address that never
existed: telling the two apart would let anyone enumerate work in progress.

## Archive, never delete

An appointment references a service **by identifier**, and this module cannot
know who holds such a reference — the same reason there are no foreign keys
across schemas. Deleting a row would leave those references pointing at
nothing.

So `IsArchived`: the service leaves the showcase and stops accepting bookings,
and stays readable for whatever already refers to it. **Identifiers are never
reused**, and archiving is a one-way door — an archived service cannot be
edited back into the catalogue, because the way back is a new service.

## Public events

In `modules/services/shared/Events/`, referenced by subscribers without ever
touching the implementation:

| Event              | Raised when                                                                    |
| ------------------ | ------------------------------------------------------------------------------ |
| `ServicePublished` | a service becomes visible to the public for the first time                     |
| `ServiceUpdated`   | an already-public service changes — **including the change that withdraws it** |
| `ServiceArchived`  | a service is withdrawn from the catalogue                                      |

Two things about them are deliberate:

- **A draft raises nothing.** Nobody outside may learn a draft exists, or a
  booking link could be built for a page the public cannot open.
- **`IsBookable` is the EFFECTIVE value** (published, bookable and not
  archived), not the flag. A subscriber can honour it without re-implementing
  this module's publication rules, and there is no separate "unpublished"
  event to forget to handle.

## Slugs

Generated from the title, stored, and editable. Once a page has been shared
its address belongs to whoever bookmarked it, so it has to survive a rewritten
title.

- A slug the administrator **typed** is honoured or refused, never altered:
  publishing `consulenza-2` to someone who asked for `consulenza` is worse
  than telling them the address is taken.
- A slug **derived** from a title is disambiguated (`-2`, `-3`, …): they did
  not ask for that address and would rather have a working page.
- Unique across the whole catalogue, **archived entries included**: a slug
  that came back to life would resurrect an address whose old content is still
  linked somewhere.
- The form **warns** — it does not forbid — when the address of a published
  page changes. There are legitimate reasons to do it; forbidding would just
  move the problem.

## Images

An image is a **file identifier plus a text alternative**. The bytes belong to
the Storage module, and this one never sees them: the browser resolves the
identifier against `GET /api/files/public/{id}`.

That endpoint is the reason this module has a `Storage` dependency in its
manifest. Uploads go out marked **`Public`** — see
[modules/storage/README.md](../storage/README.md) — because an image of the
showcase must load for a visitor with no session.

Two rules the model enforces rather than suggests:

- **The alt text is required.** It is the accessibility contract of the design
  system, and a field that may be skipped is a field that always is.
- **There is always a cover while there is at least one image.** The first
  upload becomes it; removing it promotes the next. A card with a missing
  thumbnail reads as broken, and "remember to tick the cover box" is not a
  design.

## Frontend

`@enterprise/module-services` exports the administration pages, the showcase
pages and their routes. The host wires it once:

```ts
installServicesModule({ api, apiOrigin: env.VITE_API_BASE_URL, setSeo });
```

`setSeo` is the seam that lets the detail page title itself from its data. A
route can only declare a fixed string; without it every service shared on
Slack would preview with the same generic title.

### The `/services` collision

Both this module and the Site module want that path, and two routes cannot
share one. **The composition root decides**
(`apps/web/src/router/index.ts`): when this module is installed, its pages
replace the Site module's static page. A project that does not install it
keeps the static page from `site.config.ts`, which is exactly right for a site
with nothing to book.

Neither module may make that choice, because neither knows the other exists.

### Prerender

`/services/{slug}` addresses are data, so they cannot be listed by hand:
`apps/web/prerender.config.mjs` declares them under `dynamicPublicRoutes` and
the prerender **asks the API** for them. Set `PRERENDER_API_ORIGIN` when
building statically:

```bash
PRERENDER_API_ORIGIN=http://localhost:5080 \
VITE_PUBLIC_BASE_URL=https://acme.example \
pnpm --filter @enterprise/web build:static
```

Without it — or with the API down, which in CI it usually is — only the static
routes are rendered, and the script says so. A deploy that stops because a
database was not up is a worse outcome than a sitemap missing its detail
pages.

## Configuration

None. The module has no settings: what it shows is data, not configuration.

## Not multi-tenant

It does not implement `ITenantOwned`, and that is a decision rather than an
omission: the public endpoints are **anonymous**, and an anonymous request has
no tenant to scope by. A multi-tenant deployment that needs a per-tenant
catalogue has to resolve the tenant from the host name first — a host concern,
not this module's.

## Removing it

Disable it in `module.json` (or `Modules:Services:Enabled`), drop the two
frontend imports from `apps/web`, and the application is back to the Site
module's static services page. Nothing else references it: the events have no
subscribers yet, and the dashboard tile disappears with the module because the
Dashboard module resolves providers from the container.
