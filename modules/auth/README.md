# Auth Module

Registration, login, JWT issuance, refresh-token rotation and logout — and the
**canonical template** for every module: copy this structure when creating a
new one.

## Security model

| Concern        | Decision                                                                |
| -------------- | ----------------------------------------------------------------------- |
| Access token   | JWT, 15 min, held in frontend memory only (never localStorage)          |
| Refresh token  | 256-bit random, 7 days, httpOnly SameSite=Strict cookie on `/api/auth`  |
| Storage        | Only the SHA-256 hash of refresh tokens is persisted                    |
| Rotation       | Every refresh revokes the presented token and issues a new one          |
| Reuse response | A replayed token revokes ALL the account's sessions (theft containment) |
| Passwords      | PBKDF2 via ASP.NET Identity's hasher, per-password salt                 |
| Login errors   | Identical for unknown email and wrong password (no enumeration)         |
| Brute force    | Dedicated per-IP rate limit on register/login/refresh                   |

The HOST validates tokens and enforces the authenticated-by-default fallback
policy; this module issues them. Both read the same `Jwt` configuration
section. `Jwt:SigningKey` is required (≥ 32 bytes) and NEVER committed:
development uses appsettings.Development.json, production a secret store.

## Endpoints

| Method | Route                | Auth      | Notes                            |
| ------ | -------------------- | --------- | -------------------------------- |
| POST   | `/api/auth/register` | anonymous | Creates the account and signs in |
| POST   | `/api/auth/login`    | anonymous | Rate limited per IP              |
| POST   | `/api/auth/refresh`  | cookie    | Rotates the refresh token        |
| POST   | `/api/auth/logout`   | cookie    | Revokes the session, idempotent  |
| GET    | `/api/auth/me`       | Bearer    | Caller profile (session restore) |

## Persistence

Module-owned: `AuthDbContext`, PostgreSQL schema `auth`, own migration history.
Migrations apply automatically at startup unless `Modules:Auth:AutoMigrate`
is `false` (set it in production and run migrations as a release step):

```bash
dotnet ef database update --project modules/auth/backend --startup-project apps/api/src/Api
```

## The first administrator

A fresh database has no way in: registration grants only the default role, and
promoting an account requires `roles.write`, which nobody holds yet. So the
module seeds a **bootstrap administrator** at startup.

```json
"Modules": {
  "Auth": {
    "BootstrapAdmin": {
      "Enabled": true,
      "Email": "admin@example.com",
      "DisplayName": "Admin",
      "Password": "Password123!"
    }
  }
}
```

Enabled in `appsettings.Development.json` only. **A deployment that enables it
must set its own password**: the one above is published in this repository, and
this repository is copied verbatim into every project.

How it works, and why it is not simply "create user with role Admin":

- Auth creates the ACCOUNT and publishes `BootstrapAdminSeeded` (a public
  contract event). It has no concept of a role and must not grow one.
- Authorization subscribes and grants `Admin` — see that module's README.
- The event fires **only for a newly created account**, so a restart never
  re-promotes an account somebody deliberately demoted. Everything else about
  an existing account (password included) is left untouched.
- The seeding runs on `ApplicationStarted`, not in `StartAsync`: hosted
  services run in module load order, which is a dependency graph, not a
  seeding order — Auth starts before Authorization has even migrated its
  schema.
- A password that would fail the registration policy is refused rather than
  seeded, so the account cannot be one the normal flow could never produce.

## Frontend (`@enterprise/module-auth`)

The host consumes three exports: `installAuthModule` (bootstrap),
`authRoutes` (route registry) and `useSessionStore` (header UI). The module
plugs into seams core exposes — token provider and 401-recovery handler — so
core never references the module. Session restore after a page reload works by
exchanging the refresh cookie at startup; the router guard awaits it.

## Configuration

```json
{
  "Jwt": { "SigningKey": "…", "AccessTokenMinutes": 15, "RefreshTokenDays": 7 },
  "Modules": {
    "Auth": {
      "Enabled": true,
      "AutoMigrate": true,
      "SensitivePermitLimit": 10,
      "BootstrapAdmin": { "Enabled": false }
    }
  }
}
```
