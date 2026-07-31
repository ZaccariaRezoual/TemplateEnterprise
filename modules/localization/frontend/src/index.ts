/**
 * Public surface of `@enterprise/module-localization`.
 *
 * The host calls `installLocalizationModule` at bootstrap and `loadLocale`
 * once the app starts; components use vue-i18n's `useI18n()` as usual.
 */
export {
  DEFAULT_LOCALE,
  fetchSupportedLocales,
  getStoredLocale,
  installLocalizationModule,
  loadLocale,
} from "./install";
export type { LocalizationModuleHost } from "./install";
