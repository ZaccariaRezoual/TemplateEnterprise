import { defineStore } from "pinia";
import { computed, ref } from "vue";
import type { App, Directive, DirectiveBinding } from "vue";
import { api, request } from "@/core/api/apiClient";
import { logger } from "@/core/logger/logger";

/**
 * Feature flags evaluated for the current caller.
 *
 * Core, not a module: flags govern whether a capability EXISTS in this
 * deployment, so anything in the app may read them and nothing should depend
 * on an optional module being installed to do so.
 *
 * A flag is not a permission. Permissions decide what THIS user may do and
 * are enforced by the API; flags decide what this DEPLOYMENT ships. Using a
 * flag to hide something sensitive would put a security decision in a file.
 */
export const useFeatureFlagsStore = defineStore("feature-flags", () => {
  const flags = ref<Readonly<Record<string, boolean>>>({});

  /** Whether the flags have been fetched at least once. */
  const isLoaded = ref(false);

  /**
   * Tells whether a capability is on.
   *
   * @param name Flag name, e.g. "beta.newDashboard".
   * @returns Whether it is enabled. Unknown flags are off, so a typo hides a
   * feature rather than revealing an unfinished one.
   */
  function isEnabled(name: string): boolean {
    return flags.value[name] === true;
  }

  /**
   * Loads the flags for the current caller.
   *
   * Failures leave every flag off: shipping the conservative UI beats
   * rendering a half-built feature because a request failed.
   */
  async function load(): Promise<void> {
    try {
      flags.value = await request(() => api.GET("/api/features"));
      isLoaded.value = true;
    } catch (error) {
      logger.warn("Could not load feature flags; treating every flag as off", { error });
      flags.value = {};
    }
  }

  return { flags: computed(() => flags.value), isLoaded, isEnabled, load };
});

/**
 * `v-feature` — removes an element unless the capability is on.
 *
 * ```vue
 * <NewDashboard v-feature="'beta.newDashboard'" />
 * ```
 *
 * Mirrors `v-can` from the Authorization module: it REMOVES rather than
 * hides, because an invisible element is still focusable and still announced.
 * The two are deliberately separate — one asks "does this exist here?", the
 * other "may this user do it?".
 */
export const vFeature: Directive<HTMLElement, string> = {
  mounted: applyFeature,
  updated: applyFeature,
};

function applyFeature(element: HTMLElement, binding: DirectiveBinding<string>): void {
  const store = useFeatureFlagsStore();
  const allowed = store.isEnabled(binding.value);
  const holder = element as unknown as { _vFeaturePlaceholder?: Comment };

  if (!allowed && element.parentNode !== null) {
    const marker = holder._vFeaturePlaceholder ?? document.createComment("v-feature");
    holder._vFeaturePlaceholder = marker;
    element.parentNode.replaceChild(marker, element);
    return;
  }

  const placeholder = holder._vFeaturePlaceholder;
  if (allowed && placeholder?.parentNode != null) {
    placeholder.parentNode.replaceChild(element, placeholder);
  }
}

/**
 * Registers `v-feature` on the application.
 *
 * @param app The Vue application instance.
 */
export function installFeatureFlags(app: App): void {
  app.directive("feature", vFeature);
}

/**
 * Feature checks for components.
 *
 * @returns `isEnabled` plus the raw flag map.
 *
 * @example
 * ```ts
 * const { isEnabled } = useFeatures();
 * if (isEnabled("beta.newDashboard")) { … }
 * ```
 */
export function useFeatures(): {
  isEnabled: (name: string) => boolean;
  flags: Readonly<Record<string, boolean>>;
} {
  const store = useFeatureFlagsStore();
  return {
    isEnabled: (name: string) => store.isEnabled(name),
    flags: store.flags,
  };
}
