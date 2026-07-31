/**
 * Global test setup.
 *
 * jsdom implements neither `matchMedia` (used by the theme engine) nor
 * `crypto.randomUUID` (used by the HTTP client correlation id), so both are
 * stubbed here rather than guarded for in production code.
 */
import { vi } from "vitest";

if (!("matchMedia" in globalThis)) {
  Object.defineProperty(globalThis, "matchMedia", {
    writable: true,
    value: (query: string) => ({
      matches: false,
      media: query,
      onchange: null,
      addEventListener: vi.fn(),
      removeEventListener: vi.fn(),
      dispatchEvent: vi.fn(),
    }),
  });
}

if (globalThis.crypto?.randomUUID === undefined) {
  Object.defineProperty(globalThis, "crypto", {
    writable: true,
    value: { ...globalThis.crypto, randomUUID: () => "00000000-0000-4000-8000-000000000000" },
  });
}
