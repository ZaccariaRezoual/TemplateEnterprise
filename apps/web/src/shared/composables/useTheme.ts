import { computed, ref, watchEffect, type ComputedRef, type Ref } from "vue";
import { storage } from "@/core/storage/storage";

/** Theme selectable by the user. "system" follows the OS preference. */
export type ThemePreference = "light" | "dark" | "system";

/** Theme actually applied to the document. */
export type ResolvedTheme = "light" | "dark";

const STORAGE_KEY = "theme";

/** Shared across every caller: the theme is a single application-wide value. */
const preference = ref<ThemePreference>(storage.get<ThemePreference>(STORAGE_KEY) ?? "system");

/**
 * Theme engine.
 *
 * Applies the theme by setting `data-theme` on `<html>`; every visual change
 * flows from the semantic design tokens keyed on that attribute (see
 * `assets/styles/semantic.css`). No component is theme-aware, and no component
 * needs to change to add a new theme.
 *
 * State is client-only, so it lives here (module-scoped refs), not in TanStack
 * Query. The preference is persisted so it survives reloads.
 *
 * @returns The current preference (writable), the resolved theme and a setter.
 */
export function useTheme(): {
  preference: Ref<ThemePreference>;
  theme: ComputedRef<ResolvedTheme>;
  setTheme: (next: ThemePreference) => void;
} {
  const theme = computed<ResolvedTheme>(() =>
    preference.value === "system" ? systemTheme() : preference.value,
  );

  /**
   * Selects a theme and persists the choice.
   *
   * @param next The preference to apply.
   */
  function setTheme(next: ThemePreference): void {
    preference.value = next;
    storage.set(STORAGE_KEY, next);
  }

  return { preference, theme, setTheme };
}

/**
 * Starts applying the theme to the document.
 * Called once from the application bootstrap; never from a component.
 */
export function installTheme(): void {
  watchEffect(() => {
    const resolved = preference.value === "system" ? systemTheme() : preference.value;
    document.documentElement.dataset["theme"] = resolved;
  });
}

function systemTheme(): ResolvedTheme {
  return globalThis.matchMedia?.("(prefers-color-scheme: dark)").matches ? "dark" : "light";
}
