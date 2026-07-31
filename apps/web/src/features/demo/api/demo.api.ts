import { httpClient } from "@/core/http/httpClient";
import type { IEchoResponse, IPingResponse } from "@/features/demo/types/demo.types";

/**
 * Feature service of the Demo module: the only place in the feature that knows
 * the API routes.
 *
 * Layering: component → composable → THIS service → HttpClient → Axios.
 * Components and composables never build URLs and never touch the transport.
 *
 * In Fase 3 the bodies of these functions become calls to the generated SDK;
 * because the feature only depends on this module's signatures, nothing else
 * in the feature changes.
 */
export const demoApi = {
  /**
   * Fetches the ping payload.
   *
   * @param signal Abort signal forwarded by TanStack Query on cancellation.
   * @returns The ping response.
   * @throws {import("@/core/errors/applicationError").ApplicationError} On any API failure.
   */
  ping(signal?: AbortSignal): Promise<IPingResponse> {
    return httpClient.get<IPingResponse>("/demo/ping", signal === undefined ? {} : { signal });
  },

  /**
   * Sends a text to be echoed by the API. The backend publishes a domain event
   * as a side effect (visible in the server logs).
   *
   * @param text Text to echo; the server enforces 1–500 characters.
   * @returns The echo response.
   * @throws {import("@/core/errors/applicationError").ValidationError} When the text is rejected.
   */
  echo(text: string): Promise<IEchoResponse> {
    return httpClient.post<IEchoResponse>("/demo/echo", { text });
  },
};
