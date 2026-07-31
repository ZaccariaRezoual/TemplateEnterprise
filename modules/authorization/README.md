# Authorization Module

Roles, permissions and their enforcement (RBAC + PBAC).

## The model

Authorization decisions are made on **permissions**, never on role names.
Roles are a grouping convenience that customers reorganize; permissions are
the stable contract. So an endpoint says `RequirePermission("users.read")`,
never "is this user an Admin?".

| Concept       | Owner                | Notes                                      |
| ------------- | -------------------- | ------------------------------------------ |
| Permission    | code (`Permissions`) | Closed catalogue, `resource.action`        |
| Role          | database             | Named set of permissions                   |
| Assignment    | database             | User id → role (no FK into another schema) |
| Effective set | union of roles       | Additive only; there are no deny rules     |

Deny rules are deliberately absent: they make "why can't this user do X?"
unanswerable without a debugger.

Built-in roles seeded on first run: **Administrator** (every permission) and
**User** (none). Two only — a template that ships a dozen speculative roles
forces every project to delete them.

## How it plugs in

- **Token claims**: implements `IUserClaimsEnricher`, the extension point the
  Auth module calls while issuing a token. Auth therefore knows nothing about
  roles, and disabling this module simply produces tokens without permission
  claims.
- **Default role**: subscribes to Auth's `UserRegistered` contract event and
  grants "User". Registration keeps working if this module is removed.
- **Endpoint enforcement**: `RequirePermission(...)` (in
  `Modules.Abstractions`, so any module can use it) is turned into a policy by
  the host's `PermissionPolicyProvider`, generated on demand.

Permissions live **in the access token**, so checks are stateless and cost no
database round-trip. The trade-off is staleness: a revoked permission stays
effective until the token expires (15 minutes) or is refreshed. Flows that
cannot tolerate that window must re-check in the handler.

## Endpoints

| Method | Route                                     | Permission    |
| ------ | ----------------------------------------- | ------------- |
| GET    | `/api/authorization/me`                   | authenticated |
| GET    | `/api/authorization/roles`                | `roles.read`  |
| GET    | `/api/authorization/permissions`          | `roles.read`  |
| PUT    | `/api/authorization/users/{userId}/roles` | `roles.write` |

`SetUserRoles` refuses to strip the caller's own administrator role: one
careless save must not leave a system nobody can administer.

## Frontend (`@enterprise/module-authorization`)

```ts
const { can } = usePermissions();
```

```vue
<button v-can="'users.write'">Edit</button>
<button v-can="['users.write', 'users.delete']">Manage</button>
<!-- any of -->
```

`v-can` **removes** the element rather than hiding it: an invisible element is
still focusable and still announced, which turns a hidden action into a
confusing one.

The route guard enforces `meta.permissions` (all required) and redirects to
`/forbidden`. Every client-side check is presentation only — **the API is the
security boundary**.

## Configuration

```json
{ "Modules": { "Authorization": { "Enabled": true, "AutoMigrate": true } } }
```
