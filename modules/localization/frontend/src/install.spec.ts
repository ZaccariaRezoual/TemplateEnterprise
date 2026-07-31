import { beforeEach, describe, expect, it, vi } from "vitest";
import { createApp } from "vue";
import type { I18n } from "vue-i18n";
import {
  DEFAULT_LOCALE,
  fetchSupportedLocales,
  getStoredLocale,
  installLocalizationModule,
  loadLocale,
} from "./install";

/** SDK stub answering the translations and locales endpoints. */
function mockApi(messages: Record<string, string>) {
  return {
    GET: vi.fn(async (path: string) => ({
      data:
        path === "/api/localization/locales"
          ? [
              { code: "en", name: "English" },
              { code: "it", name: "Italiano" },
            ]
          : messages,
      response: new Response(null, { status: 200 }),
    })),
  } as never;
}

function failingApi() {
  return {
    GET: vi.fn(async () => ({ error: {}, response: new Response(null, { status: 500 }) })),
  } as never;
}

/** Installs the module on a throwaway app, as the host bootstrap would. */
function install(api: unknown): I18n {
  const app = createApp({ render: () => null });
  return installLocalizationModule({ app, api: api as never });
}

/** Translates through the installed instance, as a component would. */
function translate(i18n: I18n, key: string): string {
  return (i18n.global as unknown as { t: (k: string) => string }).t(key);
}

describe("localization module", () => {
  beforeEach(() => {
    globalThis.localStorage.clear();
  });

  it("defaults to the fallback locale before any preference exists", () => {
    expect(getStoredLocale()).toBe(DEFAULT_LOCALE);
  });

  it("loads a catalogue, applies it and remembers the choice", async () => {
    const i18n = install(mockApi({ "common.save": "Salva" }));

    const applied = await loadLocale("it");

    expect(applied).toBe("it");
    expect(translate(i18n, "common.save")).toBe("Salva");
    expect(globalThis.localStorage.getItem("enterprise-i18n:locale")).toBe("it");
  });

  it("renders a missing key as the key itself", async () => {
    const i18n = install(mockApi({ "common.save": "Salva" }));
    await loadLocale("it");

    // Visibly wrong text gets reported; a silently blank label does not.
    expect(translate(i18n, "does.not.exist")).toBe("does.not.exist");
  });

  it("keeps the current locale when the catalogue cannot be fetched", async () => {
    install(failingApi());

    const applied = await loadLocale("it");

    // Untranslated beats broken: never leave the UI with empty strings.
    expect(applied).toBe(DEFAULT_LOCALE);
  });

  it("returns the supported locales for a picker", async () => {
    install(mockApi({}));

    const locales = await fetchSupportedLocales();

    expect(locales.map((locale) => locale.code)).toEqual(["en", "it"]);
  });

  it("returns an empty list rather than throwing when locales are unavailable", async () => {
    install(failingApi());

    await expect(fetchSupportedLocales()).resolves.toEqual([]);
  });
});
