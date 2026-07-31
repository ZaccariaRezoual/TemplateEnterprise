import createClient, { type Client, type ClientOptions } from "openapi-fetch";
import type { paths } from "./generated/schema";

/** Options accepted by {@link createApiClient}. */
export interface ApiClientOptions extends ClientOptions {
  /**
   * Base URL of the API. Use a same-origin path (`/api`) so the browser makes
   * same-origin requests and no CORS preflight is needed.
   */
  baseUrl: string;
}

/**
 * Typed API client. Every path, method, request body and response is checked
 * against the OpenAPI document, so a backend contract change surfaces as a
 * compile error rather than a runtime surprise.
 */
export type ApiClient = Client<paths>;

/**
 * Creates the typed API client.
 *
 * The `fetch` option is the integration seam: the application passes a fetch
 * implementation wired to its HTTP layer (correlation id, auth token, error
 * mapping), so the SDK stays free of framework concerns while every call still
 * gets the framework's behavior.
 *
 * @param options Client options; `baseUrl` is required.
 * @returns A typed client exposing GET/POST/PUT/PATCH/DELETE.
 *
 * @example
 * ```ts
 * const api = createApiClient({ baseUrl: "/api", fetch: instrumentedFetch });
 * const { data, error } = await api.GET("/api/demo/ping");
 * ```
 */
export function createApiClient(options: ApiClientOptions): ApiClient {
  return createClient<paths>(options);
}
