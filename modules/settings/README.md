# Settings Module

Typed application and per-user settings.

## Declared in code, stored selectively

Settings are **declared in `SettingKeys`**, not created at runtime. That keeps
them typed, discoverable and documented; a free-form key/value store becomes
an undocumented API within a release, and nobody can tell which keys still
matter.

Only explicitly-set values are stored. Resolution layers them:

```
caller's own value  →  global value  →  default declared in code
```

So "change the default for everyone" is a code change, not a migration, and
the table stays small regardless of user count.

## Scopes

| Scope    | Who can set it           | Endpoint                     |
| -------- | ------------------------ | ---------------------------- |
| `User`   | the user, for themselves | `PUT /api/settings/me/{key}` |
| `Global` | `settings.write` holders | `PUT /api/settings/{key}`    |

Setting a `Global`-scoped key per user is rejected: scope is part of the
declaration, not a suggestion.

`GET /api/settings` returns every setting with its effective value in one
call, so a UI configures itself without a request per key.

## Using settings from another module

Depend on `SettingsReader`, never on the table:

```csharp
var pageSize = await settingsReader.GetIntAsync(SettingKeys.ItemsPerPage, userId, ct);
```

The layering rules then exist in exactly one place.
