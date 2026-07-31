import {
  ApplicationError,
  BusinessError,
  ForbiddenError,
  NetworkError,
  NotFoundError,
  ServerError,
  UnauthorizedError,
  ValidationError,
} from "@/core/errors/applicationError";
import { parseProblemDetails } from "@/core/http/problemDetails";

/**
 * Single translation point from transport failures to the application error
 * hierarchy — the exact inverse of the backend's `GlobalExceptionHandler`.
 *
 * It is deliberately transport-agnostic (it takes a status and a body, not a
 * library-specific error object), so replacing the HTTP layer never changes
 * how errors are classified, and features can `catch (e) { if (e instanceof
 * ForbiddenError) … }` without knowing what performs the request.
 *
 * @param status HTTP status code of the response.
 * @param body Parsed response body; expected to be ProblemDetails.
 * @param cause Underlying error, preserved for logging.
 * @returns The matching {@link ApplicationError} subclass.
 */
export function mapResponseToApplicationError(
  status: number,
  body: unknown,
  cause?: unknown,
): ApplicationError {
  const problem = parseProblemDetails(body);
  const message = problem?.detail ?? problem?.title ?? `Request failed with status ${status}.`;
  const options = { cause, correlationId: problem?.correlationId };

  switch (status) {
    case 400:
      return new ValidationError(message, problem?.errors ?? {}, options);
    case 401:
      return new UnauthorizedError(message, options);
    case 403:
      return new ForbiddenError(message, options);
    case 404:
      return new NotFoundError(message, options);
    case 422:
      return new BusinessError(message, options);
    default:
      return status >= 500
        ? new ServerError(message, options)
        : new ApplicationError(message, options);
  }
}

/**
 * Maps a failure that produced no response at all — offline, DNS failure,
 * timeout, or a request aborted by the browser.
 *
 * Kept separate from status mapping because these are usually retryable and
 * deserve a different message than a server rejection.
 *
 * @param cause The thrown error.
 * @returns A {@link NetworkError}, or the original error when it is already an
 * {@link ApplicationError}.
 */
export function mapTransportFailure(cause: unknown): ApplicationError {
  if (cause instanceof ApplicationError) {
    return cause;
  }

  const isTimeout = cause instanceof DOMException && cause.name === "TimeoutError";
  const message = isTimeout
    ? "The request timed out. Please try again."
    : "Could not reach the server. Check your connection.";

  return new NetworkError(message, { cause });
}
