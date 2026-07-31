/**
 * Public surface of `@enterprise/sdk`.
 *
 * Exposes the client factory plus the generated schema types, so features can
 * type their own code against API contracts (`components["schemas"]["..."]`)
 * without importing from the generated file directly.
 */
export { createApiClient } from "./client";
export type { ApiClient, ApiClientOptions } from "./client";
export type { components, operations, paths } from "./generated/schema";
