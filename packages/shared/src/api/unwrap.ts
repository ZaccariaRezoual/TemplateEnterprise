import { mapResponseToApplicationError, mapTransportFailure } from "./errorMapper";

/** Shape returned by every openapi-fetch call (the generated SDK's transport). */
export interface SdkResult<TData> {
  data?: TData;
  error?: unknown;
  response: Response;
}

/**
 * Converts an SDK result into a value or a typed, thrown error.
 *
 * The SDK reports failures as a value (`{ error }`), while TanStack Query and
 * idiomatic `try`/`catch` expect a rejection: this is the single bridge
 * between the two conventions, and the single place that classifies API
 * failures into the shared error hierarchy. Both the application's request
 * wrapper and module frontends build on it, so every consumer classifies
 * errors identically.
 *
 * @typeParam TData Expected payload.
 * @param result The settled SDK result.
 * @returns The response payload.
 * @throws {ApplicationError} A typed error ({@link import("../errors/applicationError").ValidationError},
 * {@link import("../errors/applicationError").UnauthorizedError}, ...).
 */
export function unwrapSdkResult<TData>(result: SdkResult<TData>): TData {
  if (result.error !== undefined || !result.response.ok) {
    throw mapResponseToApplicationError(result.response.status, result.error, result.error);
  }
  return result.data as TData;
}

/**
 * Executes an SDK call and unwraps it, mapping transport failures (offline,
 * timeout, aborted) to {@link import("../errors/applicationError").NetworkError}.
 *
 * @typeParam TData Expected payload.
 * @param execute Factory performing the SDK call (a factory, not a promise, so
 * callers implementing retry can execute it again).
 * @returns The response payload.
 * @throws {ApplicationError} Any transport or API failure, already mapped.
 */
export async function executeSdkCall<TData>(
  execute: () => Promise<SdkResult<TData>>,
): Promise<TData> {
  let result: SdkResult<TData>;
  try {
    result = await execute();
  } catch (cause) {
    throw mapTransportFailure(cause);
  }
  return unwrapSdkResult(result);
}
