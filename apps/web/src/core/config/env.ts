import { z } from "zod";

/**
 * Schema of the environment variables consumed by the application.
 *
 * Everything the app reads from `import.meta.env` is declared here so a missing
 * or malformed variable fails loudly at startup instead of producing an
 * undefined deep inside a feature at runtime.
 */
const envSchema = z.object({
  /**
   * ORIGIN of the API — not a path prefix. The generated SDK's paths already
   * contain the full route (`/api/demo/ping`), so this must stay empty for a
   * same-origin deployment (the dev server proxies `/api` to the backend).
   * Set it to an absolute origin like "https://api.example.com" only when the
   * API is served from another host.
   */
  VITE_API_BASE_URL: z.string().default(""),
  /** Request timeout in milliseconds. */
  VITE_API_TIMEOUT_MS: z.coerce.number().int().positive().default(15_000),
  /** Minimum level the logger emits. */
  VITE_LOG_LEVEL: z.enum(["debug", "info", "warn", "error", "silent"]).default("info"),
});

/** Validated, immutable application environment. */
export type AppEnv = Readonly<z.infer<typeof envSchema>>;

/**
 * Parses and validates the build-time environment.
 *
 * @param source Raw environment record (defaults to Vite's `import.meta.env`).
 * @returns The validated environment.
 * @throws {Error} When a variable is missing or does not match the schema;
 * the message lists every offending key so misconfiguration is obvious.
 */
export function parseEnv(source: Record<string, unknown> = import.meta.env): AppEnv {
  const result = envSchema.safeParse(source);

  if (!result.success) {
    const issues = result.error.issues
      .map((issue) => `${issue.path.join(".")}: ${issue.message}`)
      .join("; ");
    throw new Error(`Invalid environment configuration — ${issues}`);
  }

  return Object.freeze(result.data);
}

/**
 * The validated environment of the running application.
 * Import this instead of touching `import.meta.env` directly.
 */
export const env: AppEnv = parseEnv();
