import { api, request } from "@/core/api/apiClient";
import type { IEchoResponse, IPingResponse } from "@/features/demo/types/demo.types";

/**
 * Feature service of the Demo module: the only place in the feature that knows
 * which API operations it uses.
 *
 * Layering: component → composable → THIS service → SDK → API.
 * Components and composables never call the SDK and never build URLs. The
 * paths below are checked against the OpenAPI document at compile time, so a
 * renamed endpoint breaks the build instead of production.
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
    return request(api.GET("/api/demo/ping", signal === undefined ? {} : { signal }));
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
    return request(api.POST("/api/demo/echo", { body: { text } }));
  },
};
