import type { ApiClient } from "@enterprise/sdk";
import { executeSdkCall } from "@enterprise/shared";
import type { App } from "vue";
import { createI18n, type I18n } from "vue-i18n";

/** Locale tag used before a preference is known and when one is unsupported. */
export const DEFAULT_LOCALE = "en";

const STORAGE_KEY = "enterprise-i18n:locale";

let i18n: I18n | undefined;
let configuredApi: ApiClient | undefined;

/**
 * Reads the stored locale preference.
 *
 * Guarded: storage throws in private-browsing modes, and failing to remember
 * a language must never stop the application from starting.
 *
 * @returns The stored locale, or the default.
 */
export function getStoredLocale(): string {
  try {
    return globalThis.localStorage?.getItem(STORAGE_KEY) ?? DEFAULT_LOCALE;
  } catch {
    return DEFAULT_LOCALE;
  }
}

function storeLocale(locale: string): void {
  try {
    globalThis.localStorage?.setItem(STORAGE_KEY, locale);
  } catch {
    // Persisting the preference is best-effort by design.
  }
}

/** Integration seams the HOST exposes and this module plugs into. */
export interface LocalizationModuleHost {
  /** The Vue application, which receives the i18n plugin. */
  app: App;
  /** The application's configured SDK client. */
  api: ApiClient;
}

/**
 * Wires vue-i18n into the host application.
 *
 * Catalogues are FETCHED from the API rather than bundled, so server messages
 * and UI strings come from one source and a translation fix ships without a
 * frontend build. The plugin is installed synchronously with an empty
 * catalogue and filled by {@link loadLocale}, so a slow network delays
 * translations, never the first paint.
 *
 * @param host The host integration seams.
 * @returns The i18n instance, so tests and advanced hosts can inspect or
 * extend it without this module keeping a hidden global.
 */
export function installLocalizationModule(host: LocalizationModuleHost): I18n {
  configuredApi = host.api;

  i18n = createI18n({
    legacy: false,
    locale: getStoredLocale(),
    fallbackLocale: DEFAULT_LOCALE,
    messages: {},
    // A missing key renders as the key itself: visibly wrong text gets
    // reported, a silently blank label does not.
    missingWarn: false,
    fallbackWarn: false,
  });

  host.app.use(i18n);
  return i18n;
}

/**
 * Loads a locale's catalogue and switches to it.
 *
 * @param locale Locale tag to load; falls back to the default when unknown.
 * @returns The locale actually applied.
 */
export async function loadLocale(locale: string = getStoredLocale()): Promise<string> {
  if (i18n === undefined || configuredApi === undefined) {
    throw new Error(
      "Localization module is not installed. Call installLocalizationModule() at bootstrap.",
    );
  }

  try {
    const messages = await executeSdkCall(() =>
      configuredApi!.GET("/api/localization/translations/{locale}", {
        params: { path: { locale } },
      }),
    );

    i18n.global.setLocaleMessage(locale, messages as Record<string, string>);
    (i18n.global.locale as unknown as { value: string }).value = locale;
    storeLocale(locale);
    return locale;
  } catch {
    // Untranslated beats broken: keep whatever is already loaded.
    return (i18n.global.locale as unknown as { value: string }).value;
  }
}

/**
 * Lists the locales the API supports, for a language picker.
 *
 * @returns The supported locales, or an empty list when unavailable.
 */
export async function fetchSupportedLocales(): Promise<
  ReadonlyArray<{ code: string; name: string }>
> {
  if (configuredApi === undefined) {
    return [];
  }

  return executeSdkCall(() => configuredApi!.GET("/api/localization/locales")).catch(() => []);
}
