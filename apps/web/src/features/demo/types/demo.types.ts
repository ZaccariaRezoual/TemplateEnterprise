import type { components } from "@enterprise/sdk";

/**
 * Contracts of the Demo feature.
 *
 * They are ALIASES of the generated SDK schemas, never hand-written copies:
 * if the backend changes a contract, regenerating the SDK turns the mismatch
 * into a compile error here instead of a runtime surprise in a component.
 */

/** Response of `GET /api/demo/ping`. */
export type IPingResponse = components["schemas"]["PingResponse"];

/** Response of `POST /api/demo/echo`. */
export type IEchoResponse = components["schemas"]["EchoResponse"];
