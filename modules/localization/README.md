# Localization Module

Supported locales and their translation catalogues.

## Resources, not database rows

Translations are **embedded resources** shipped with the build. That makes a
deployment reproducible, puts translation changes through code review, and
keeps a missing key a build-time artifact rather than a production surprise.

Projects needing translator-editable content at runtime add a
database-backed catalogue behind the same endpoint — the frontend does not
change.

## One source for server and client

The API serves the catalogue and the frontend feeds it to vue-i18n, so
server-side messages and UI strings come from the same file. Catalogues are
fetched rather than bundled: the app paints first and gets its strings a
moment later, and a translation fix ships without a frontend build.

## Endpoints

| Method | Route                                     | Auth      |
| ------ | ----------------------------------------- | --------- |
| GET    | `/api/localization/locales`               | anonymous |
| GET    | `/api/localization/translations/{locale}` | anonymous |

Anonymous on purpose: the sign-in screen must be translated before anyone can
authenticate.

An unknown locale falls back to English rather than returning nothing — an
untranslated UI beats an empty one. A missing key renders as the key itself,
for the same reason.

## Adding a language

1. Add `Resources/<code>.json` with the same keys as `en.json`.
2. Add the locale to `SupportedLocales` in `LocalizationModule`.

Nothing else: the catalogue loader discovers resources by convention.

## Frontend (`@enterprise/module-localization`)

```ts
installLocalizationModule({ app, api });
await loadLocale(); // uses the stored preference
```

Components then use vue-i18n's `useI18n()` as usual.
