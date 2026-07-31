import { logger } from "@/core/logger/logger";

/**
 * Namespaced, fail-safe wrapper over the Web Storage API.
 *
 * Why it exists: `localStorage` throws in private-browsing modes and when the
 * quota is exceeded, and returns raw strings. This wrapper never throws (a
 * storage failure must not break the UI), namespaces keys to avoid collisions
 * with other apps on the same origin, and handles JSON serialization.
 */
export class Storage {
  #prefix: string;
  #backend: globalThis.Storage | undefined;

  /**
   * @param prefix Key namespace (defaults to "ef").
   * @param backend Underlying storage; defaults to `localStorage` when available.
   */
  constructor(prefix = "ef", backend: globalThis.Storage | undefined = safeLocalStorage()) {
    this.#prefix = prefix;
    this.#backend = backend;
  }

  /**
   * Reads and deserializes a value.
   *
   * @param key Unprefixed key.
   * @returns The stored value, or `undefined` when absent or unreadable.
   */
  get<T>(key: string): T | undefined {
    const raw = this.#backend?.getItem(this.#key(key));
    if (raw === null || raw === undefined) {
      return undefined;
    }

    try {
      return JSON.parse(raw) as T;
    } catch (error) {
      logger.warn("Discarding unparsable storage entry", { key, error });
      this.remove(key);
      return undefined;
    }
  }

  /**
   * Serializes and stores a value. Failures are logged, never thrown.
   *
   * @param key Unprefixed key.
   * @param value Value to persist (must be JSON-serializable).
   */
  set(key: string, value: unknown): void {
    try {
      this.#backend?.setItem(this.#key(key), JSON.stringify(value));
    } catch (error) {
      logger.warn("Could not write to storage", { key, error });
    }
  }

  /**
   * Removes a value.
   *
   * @param key Unprefixed key.
   */
  remove(key: string): void {
    try {
      this.#backend?.removeItem(this.#key(key));
    } catch (error) {
      logger.warn("Could not remove storage entry", { key, error });
    }
  }

  #key(key: string): string {
    return `${this.#prefix}:${key}`;
  }
}

function safeLocalStorage(): globalThis.Storage | undefined {
  try {
    return globalThis.localStorage;
  } catch {
    // Blocked by browser privacy settings: degrade to a no-op storage.
    return undefined;
  }
}

/** Shared storage instance for application-wide persisted state. */
export const storage = new Storage();
