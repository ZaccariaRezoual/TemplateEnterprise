# Users Module

Administration of user profiles.

## Why it owns a projection

Accounts belong to the **Auth** module. This module keeps its own `users`
schema, populated by subscribing to Auth's `UserRegistered` contract event.

Duplicating email and display name is deliberate. It lets Users query, sort,
search and page without reaching into another module's tables — which is what
keeps modules independently removable and separately deployable later. The
alternative (joining across schemas) turns every module boundary into a
dependency that only shows up when you try to delete something.

The projection handler is idempotent, so replaying an event never creates
duplicates.

## Endpoints

| Method | Route             | Permission    | Notes                           |
| ------ | ----------------- | ------------- | ------------------------------- |
| GET    | `/api/users`      | `users.read`  | Paged, searchable (email/name)  |
| PUT    | `/api/users/{id}` | `users.write` | Display name, job title, active |

Page size is capped at 200 by the validator: an uncapped page size is a
denial-of-service vector on any list endpoint.

## Frontend (`@enterprise/module-users`)

Exports `usersRoutes` and `installUsersModule`. The list page shows an
explicit "you do not have permission" state instead of an empty table — an
empty table reads as "there are no users", which would be a lie.

## Dependencies

Declares `Auth` (events feed the projection) and `Authorization` (permissions
protect the endpoints) in `module.json`; the loader refuses to start if either
is disabled.
