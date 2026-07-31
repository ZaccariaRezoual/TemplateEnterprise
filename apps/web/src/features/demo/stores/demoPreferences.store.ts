import { defineStore } from "pinia";
import { ref } from "vue";

/**
 * Client-side preferences of the Demo feature.
 *
 * Exists to show WHAT belongs in Pinia: UI state owned by the client, which no
 * API knows about. Anything coming from the server lives in TanStack Query
 * instead (see `useDemo`) — never duplicate server data here.
 */
export const useDemoPreferencesStore = defineStore("demo-preferences", () => {
  /** Whether the echo history panel is expanded. */
  const isHistoryVisible = ref(false);

  /** Texts echoed in this session, most recent first (client-only). */
  const history = ref<string[]>([]);

  /** Toggles the history panel. */
  function toggleHistory(): void {
    isHistoryVisible.value = !isHistoryVisible.value;
  }

  /**
   * Records an echoed text, keeping the last five entries.
   *
   * @param text The text that was echoed.
   */
  function remember(text: string): void {
    history.value = [text, ...history.value].slice(0, 5);
  }

  return { isHistoryVisible, history, toggleHistory, remember };
});
