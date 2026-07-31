import { computed, ref, watchEffect, type ComputedRef, type Ref } from "vue";

/** Theme selectable by the user. "system" follows the OS preference. */
export type ThemePreference = "light" | "dark" | "system";

/** Theme actually applied to the document. */
export type ResolvedTheme = "light" | "dark";

const STORAGE_KEY = "enterprise-ui:theme";

/** Shared across every caller: the theme is a single application-wide value. */
const preference = ref<ThemePreference>(readStoredPreference());

/** Tracks the OS setting so "system" reacts to changes without a reload. */
const systemPrefersDark = ref(prefersDark());

/**
 * Theme engine of the design system.
 *
 * Applies the theme by setting `data-theme` on `<html>`; every visual change
 * flows from the semantic tokens keyed on that attribute
 * (`@enterprise/ui/tokens.css`). No component is theme-aware, and adding a
 * theme requires no component change — only a new set of semantic overrides.
 *
 * The preference is client state, so it lives here (module-scoped refs) and is
 * persisted; it never belongs in a server-state cache.
 *
 * @returns The current preference (writable), the resolved theme and a setter.
 */
export function useTheme(): {
  preference: Ref<ThemePreference>;
  theme: ComputedRef<ResolvedTheme>;
  setTheme: (next: ThemePreference) => void;
} {
  const theme = computed<ResolvedTheme>(() => resolve());

  /**
   * Selects a theme and persists the choice.
   *
   * @param next The preference to apply.
   */
  function setTheme(next: ThemePreference): void {
    preference.value = next;
    writeStoredPreference(next);
  }

  return { preference, theme, setTheme };
}

/**
 * Starts applying the theme to the document and tracking the OS preference.
 *
 * Call once from the application bootstrap — never from a component, which
 * would attach a new media-query listener on every mount.
 *
 * @returns A function that stops watching the OS preference.
 */
export function installTheme(): () => void {
  watchEffect(() => {
    document.documentElement.dataset["theme"] = resolve();
  });

  const query = globalThis.matchMedia?.("(prefers-color-scheme: dark)");
  if (query === undefined) {
    return () => {};
  }

  const onChange = (event: MediaQueryListEvent): void => {
    systemPrefersDark.value = event.matches;
  };
  query.addEventListener("change", onChange);
  return () => {
    query.removeEventListener("change", onChange);
  };
}

function resolve(): ResolvedTheme {
  if (preference.value !== "system") {
    return preference.value;
  }
  return systemPrefersDark.value ? "dark" : "light";
}

function prefersDark(): boolean {
  return globalThis.matchMedia?.("(prefers-color-scheme: dark)").matches ?? false;
}

// Storage access is guarded: it throws in private-browsing modes, and failing
// to remember a theme must never break the application.
function readStoredPreference(): ThemePreference {
  try {
    const stored = globalThis.localStorage?.getItem(STORAGE_KEY);
    return stored === "light" || stored === "dark" || stored === "system" ? stored : "system";
  } catch {
    return "system";
  }
}

function writeStoredPreference(value: ThemePreference): void {
  try {
    globalThis.localStorage?.setItem(STORAGE_KEY, value);
  } catch {
    // Persisting the preference is best-effort by design.
  }
}
