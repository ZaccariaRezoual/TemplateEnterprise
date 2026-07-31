import { createApiClient, type ApiClient } from "@enterprise/sdk";
import {
  executeSdkCall,
  UnauthorizedError,
  type ApplicationError,
  type SdkResult,
} from "@enterprise/shared";
import { env } from "@/core/config/env";
import { logger } from "@/core/logger/logger";

/**
 * The application's single API transport and its integration seams.
 *
 * Responsibilities:
 * - Configures the generated SDK from the validated environment.
 * - Attaches the correlation id and, when a provider is registered, the
 *   Bearer token to every request.
 * - Recovers expired sessions: on 401 it asks the registered handler to
 *   refresh, then retries the call exactly once.
 *
 * The seams (`setAuthTokenProvider`, `setUnauthorizedHandler`) exist so the
 * AUTH MODULE can plug in without core depending on it: with the module
 * absent, no provider is registered and every call is anonymous. Modules plug
 * into core; core never imports a module.
 */
const client: ApiClient = createApiClient({
  baseUrl: env.VITE_API_BASE_URL,
  headers: { Accept: "application/json" },
});

/** Returns the current access token, or undefined when signed out. */
type AuthTokenProvider = () => string | undefined;

/** Tries to recover the session; returns true when the call may be retried. */
type UnauthorizedHandler = () => Promise<boolean>;

let authTokenProvider: AuthTokenProvider | undefined;
let unauthorizedHandler: UnauthorizedHandler | undefined;

/**
 * Registers the access-token source (called by the auth module at bootstrap).
 *
 * @param provider Returns the in-memory access token, if any.
 */
export function setAuthTokenProvider(provider: AuthTokenProvider): void {
  authTokenProvider = provider;
}

/**
 * Registers the session-recovery handler (called by the auth module at
 * bootstrap). The handler typically exchanges the refresh cookie for a new
 * access token.
 *
 * @param handler Resolves true when the session was recovered.
 */
export function setUnauthorizedHandler(handler: UnauthorizedHandler): void {
  unauthorizedHandler = handler;
}

client.use({
  onRequest({ request }) {
    // Correlation id first: a user action is traceable from the browser to
    // the server logs (Seq) with one value.
    request.headers.set("X-Correlation-ID", crypto.randomUUID());

    const token = authTokenProvider?.();
    if (token !== undefined) {
      request.headers.set("Authorization", `Bearer ${token}`);
    }
    return request;
  },
});

/**
 * The typed API client. Feature services use it; components and composables
 * never do.
 */
export const api = client;

/**
 * Executes an SDK call, mapping failures to the shared error hierarchy and
 * transparently retrying ONCE after a successful session refresh.
 *
 * @typeParam TData Expected payload.
 * @param execute Factory performing the SDK call. A factory — not a promise —
 * because a consumed request cannot be re-sent on retry.
 * @returns The response payload.
 * @throws {ApplicationError} A typed error when the call (and any retry) fails.
 */
export async function request<TData>(execute: () => Promise<SdkResult<TData>>): Promise<TData> {
  try {
    return await executeSdkCall(execute);
  } catch (error) {
    // Local capture: the module-level variable is mutable, so TypeScript
    // cannot keep it narrowed across the await below.
    const handler = unauthorizedHandler;

    if (error instanceof UnauthorizedError && handler !== undefined && (await handler())) {
      // Session refreshed: the retried call carries the new token.
      return await executeSdkCall(execute).catch((retryError: unknown) => {
        throw logAndReturn(retryError as ApplicationError);
      });
    }

    throw logAndReturn(error as ApplicationError);
  }
}

function logAndReturn(error: ApplicationError): ApplicationError {
  logger.error("API request failed", {
    type: error.name,
    message: error.message,
    correlationId: error.correlationId,
  });
  return error;
}
