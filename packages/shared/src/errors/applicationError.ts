/**
 * Frontend error hierarchy — the mirror image of the backend one
 * (`AppException` and subclasses), so a failure keeps the same meaning on both
 * sides of the wire.
 *
 * Features never inspect HTTP status codes or Axios internals: the HTTP client
 * translates every failed response into one of these classes, and UI code
 * branches on the error type instead.
 */

/** Root of the hierarchy. Every expected application failure derives from it. */
export class ApplicationError extends Error {
  /**
   * @param message Human-readable, client-safe description.
   * @param options.cause Underlying error, preserved for logging.
   * @param options.correlationId Correlation id returned by the API, used for support.
   */
  constructor(
    message: string,
    options: { cause?: unknown; correlationId?: string | undefined } = {},
  ) {
    super(message, options.cause === undefined ? undefined : { cause: options.cause });
    this.name = new.target.name;
    this.correlationId = options.correlationId;
  }

  /** Correlation id of the failed request, when the API provided one. */
  readonly correlationId: string | undefined;
}

/**
 * Input validation failed (HTTP 400). Carries per-field messages so forms can
 * highlight the offending inputs.
 */
export class ValidationError extends ApplicationError {
  /**
   * @param message Summary message.
   * @param errors Failed fields: key = field name, value = messages.
   * @param options.cause Underlying error.
   * @param options.correlationId Correlation id returned by the API.
   */
  constructor(
    message: string,
    errors: Readonly<Record<string, readonly string[]>>,
    options: { cause?: unknown; correlationId?: string | undefined } = {},
  ) {
    super(message, options);
    this.errors = errors;
  }

  /** Failed fields: key = field name, value = error messages. */
  readonly errors: Readonly<Record<string, readonly string[]>>;
}

/** A business rule was violated (HTTP 422). The message is safe to display. */
export class BusinessError extends ApplicationError {}

/** The caller is not authenticated (HTTP 401): trigger sign-in or refresh. */
export class UnauthorizedError extends ApplicationError {}

/** The caller lacks the required role/permission (HTTP 403). */
export class ForbiddenError extends ApplicationError {}

/** The requested resource does not exist (HTTP 404). */
export class NotFoundError extends ApplicationError {}

/**
 * The caller sent too many requests and was rate limited (HTTP 429).
 *
 * It has no counterpart in the backend exception hierarchy because nothing
 * throws it: the limit is enforced by middleware before a handler runs. It
 * exists here because the CLIENT has to tell it apart — "slow down" is a
 * different message, and a different remedy, from "something went wrong".
 */
export class RateLimitedError extends ApplicationError {}

/**
 * The request never produced a usable response: offline, DNS failure, timeout
 * or CORS rejection. Distinct from server errors because it is usually
 * retryable and warrants a different message ("check your connection").
 */
export class NetworkError extends ApplicationError {}

/** The server failed unexpectedly (HTTP 5xx). Details stay server-side. */
export class ServerError extends ApplicationError {}
