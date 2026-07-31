import { createApiClient, type ApiClient } from "@enterprise/sdk";
import { env } from "@/core/config/env";
import type { ApplicationError } from "@/core/errors/applicationError";
import { mapResponseToApplicationError, mapTransportFailure } from "@/core/http/errorMapper";
import { logger } from "@/core/logger/logger";

/**
 * The application's single API transport.
 *
 * Responsibilities:
 * - Configures the generated SDK with the base URL and timeout from the
 *   validated environment.
 * - Attaches the correlation id to every request, so one user action can be
 *   traced from the browser console to the server logs (Seq) with one value.
 *
 * Fase 4 adds the auth token and refresh handling here — one place, applied to
 * every call in the application by construction.
 */
const client: ApiClient = createApiClient({
  baseUrl: env.VITE_API_BASE_URL,
  headers: { Accept: "application/json" },
});

client.use({
  onRequest({ request }) {
    request.headers.set("X-Correlation-ID", crypto.randomUUID());
    return request;
  },
});

/**
 * The typed API client. Feature services use it; components and composables
 * never do.
 */
export const api = client;

/** Shape returned by every openapi-fetch call. */
interface SdkResult<TData> {
  data?: TData;
  error?: unknown;
  response: Response;
}

/**
 * Converts an SDK result into a value or a typed error.
 *
 * The SDK reports failures as a value (`{ error }`) rather than by throwing.
 * TanStack Query — and idiomatic `try`/`catch` — expect a rejection, so this
 * is the one place that bridges the two conventions, and the only place that
 * classifies API failures.
 *
 * @typeParam TData Expected payload.
 * @param call The pending SDK call.
 * @returns The response payload.
 * @throws {ApplicationError} A typed error: {@link ValidationError},
 * {@link UnauthorizedError}, {@link NetworkError} and friends.
 */
export async function request<TData>(call: Promise<SdkResult<TData>>): Promise<TData> {
  let result: SdkResult<TData>;

  try {
    result = await call;
  } catch (cause) {
    // No response at all: offline, DNS, CORS or an aborted request.
    throw logAndReturn(mapTransportFailure(cause));
  }

  if (result.error !== undefined || !result.response.ok) {
    throw logAndReturn(
      mapResponseToApplicationError(result.response.status, result.error, result.error),
    );
  }

  return result.data as TData;
}

function logAndReturn(error: ApplicationError): ApplicationError {
  logger.error("API request failed", {
    type: error.name,
    message: error.message,
    correlationId: error.correlationId,
  });
  return error;
}
