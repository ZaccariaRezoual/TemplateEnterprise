# Creating a project from the template

This repository is a **template**, not a dependency. A new project gets its own
copy of the code and owns it from day one: no upstream release can break it,
and it can change anything — including the framework itself.

```bash
node scripts/create-project.mjs --name "Acme CRM" --target ../acme-crm
```

Options:

| Option      | Meaning                                                               |
| ----------- | --------------------------------------------------------------------- |
| `--name`    | Product name. Everything else is derived from it.                     |
| `--target`  | Directory to create. Must not exist, or must be empty.                |
| `--scope`   | npm scope for the workspace packages. Default: derived from `--name`. |
| `--dry-run` | Report what would happen without writing anything.                    |

## What it renames

| Template               | `--name "Acme CRM"` | Where                              |
| ---------------------- | ------------------- | ---------------------------------- |
| `EnterpriseFramework`  | `AcmeCrm`           | .NET namespaces, assemblies, paths |
| `@enterprise/…`        | `@acme-crm/…`       | workspace packages and imports     |
| `enterprise-framework` | `acme-crm`          | Docker Compose project, containers |
| `Enterprise Framework` | `Acme CRM`          | product name in the UI             |

Only files **tracked by git** are copied. That is what keeps `node_modules`,
`bin/`, `obj/` and local settings out of the new repository, without a second
ignore list here that would drift from `.gitignore`.

The scaffolder itself is not copied: a project is created once, and shipping
the script invites a second run inside a repository that is no longer the
template.

## After scaffolding

```bash
cd ../acme-crm
git init && git add -A && git commit -m "chore: initial commit from template"
pnpm install
dotnet build
docker compose -f docker/docker-compose.yml up -d
pnpm --filter @acme-crm/web dev
```

Then make it yours:

1. **Brand** — add `packages/ui/src/styles/themes/<brand>.css` overriding the
   _semantic_ tokens, and document it in `docs/design-system.md`. Nothing else
   should need to change to rebrand; if a component resists, the component has
   a tokenisation gap — fix the component (see `docs/design-system.md`).
2. **Modules** — add your business modules under `modules/`. The framework
   modules (auth, authorization, users, audit, settings, email, storage,
   notifications, localization, realtime, dashboard) stay. Two are decisions
   rather than defaults:
   - `services` serves `/services` from the database and replaces the static
     page in `site.config.ts`. A product with nothing to publish disables it
     (`Modules:Services:Enabled`) and drops its two imports from `apps/web` —
     the static page comes back on its own.
   - `appointments` adds public booking and the calendar, and depends on
     `services`. A product that does not take bookings disables it
     (`Modules:Appointments:Enabled`) and drops its imports. If you keep it,
     **set `Modules:Appointments:TimeZone`**: it decides what "we open at 9"
     means, and the default is Europe/Rome.
3. **Dashboard** — implement `IDashboardWidgetProvider` in each new module so
   the landing page reflects your product (see `modules/dashboard/README.md`).
4. **Public site** — rewrite `apps/web/src/site.config.ts`: it holds every word
   of the home, about, contact and privacy pages (its `services` section is
   only the fallback used when the Services module is disabled). Three of them
   are not optional before going live:
   - `contact.email` and `Modules:Site:ContactRecipient`, or messages go to an
     address that belongs to nobody;
   - `privacy`, because a site collecting a name and an email needs a real
     notice — the placeholder is worse than none;
   - `VITE_PUBLIC_BASE_URL`, without which the canonical URL, the sitemap and
     the `robots.txt` sitemap line are all skipped.

   A project that is only an internal tool deletes the `installSiteModule`
   call and the `siteRoutes` import instead: the root then goes straight to
   `/admin`.

5. **Publish the public pages as static HTML** with
   `pnpm --filter @acme-crm/web build:static`. Link unfurlers do not run
   JavaScript, so without this step every shared link shows the same empty
   preview.

   Pages whose address is data — a service is `/services/<slug>` — need
   `PRERENDER_API_ORIGIN` pointing at a reachable API, or only the static
   routes are rendered (the script says so rather than failing).

## Removing the Demo module

The scaffolder deliberately leaves the Demo module in place. It is not dead
weight while you are learning the wiring, and it cannot be removed
mechanically: the realtime e2e test uses the demo page's "Notify me" button as
its only trigger for a server-sent notification, so a script that deleted the
module would hand over a repository whose test suite no longer passes.

When you no longer need it, remove it deliberately — it is a short list:

1. Delete `modules/demo/`, `apps/web/src/features/demo/`,
   `apps/web/e2e/demo.spec.ts` and
   `apps/api/tests/IntegrationTests/DemoModuleTests.cs`.
2. `dotnet sln remove modules/demo/backend/<Name>.Modules.Demo.csproj` and drop
   the `ProjectReference` from `apps/api/src/Api` and from the test projects.
3. In `apps/api/src/Api/Program.cs`, remove the `using` and the
   `typeof(DemoModule).Assembly` entry.
4. In `apps/web/src/router/index.ts`, remove the `demoRoutes` import and spread.
5. In `apps/web/src/layouts/AdminLayout.vue`, remove the `Demo` navigation
   entry and the now-unused `BeakerIcon` import.
6. In `apps/web/e2e/realtime.spec.ts` and `responsive.spec.ts`, replace the
   `page.goto("/admin/demo")` calls: realtime needs some other endpoint that raises a
   notification, and the responsive "last control" test needs any page long
   enough to scroll.

`dotnet build`, `pnpm -r typecheck` and `pnpm test` will tell you if you missed
a reference.
