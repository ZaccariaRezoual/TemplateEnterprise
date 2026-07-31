import { AxiosError } from "axios";
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
 * This is what allows features to `catch (e) { if (e instanceof ForbiddenError) ... }`
 * without ever importing Axios or knowing about status codes.
 *
 * @param error The error thrown by the transport layer.
 * @returns The matching {@link ApplicationError} subclass. Non-Axios errors are
 * wrapped in a generic {@link ApplicationError} so callers always get one type.
 */
export function mapToApplicationError(error: unknown): ApplicationError {
  if (error instanceof ApplicationError) {
    return error;
  }

  if (!(error instanceof AxiosError)) {
    return new ApplicationError("An unexpected error occurred.", { cause: error });
  }

  // No response at all: offline, timeout, DNS or CORS failure.
  if (error.response === undefined) {
    const message =
      error.code === "ECONNABORTED"
        ? "The request timed out. Please try again."
        : "Could not reach the server. Check your connection.";
    return new NetworkError(message, { cause: error });
  }

  const { status, data } = error.response;
  const problem = parseProblemDetails(data);
  const correlationId = problem?.correlationId;
  const message = problem?.detail ?? problem?.title ?? error.message;
  const options = { cause: error, correlationId };

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
