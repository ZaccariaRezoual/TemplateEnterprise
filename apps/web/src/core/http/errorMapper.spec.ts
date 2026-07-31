import { describe, expect, it } from "vitest";
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
import { mapResponseToApplicationError, mapTransportFailure } from "@/core/http/errorMapper";

describe("mapResponseToApplicationError", () => {
  it.each([
    [401, UnauthorizedError],
    [403, ForbiddenError],
    [404, NotFoundError],
    [422, BusinessError],
    [500, ServerError],
    [503, ServerError],
  ])("maps HTTP %i to the matching error type", (status, expected) => {
    expect(mapResponseToApplicationError(status, {})).toBeInstanceOf(expected);
  });

  it("maps 400 to a ValidationError carrying the field errors", () => {
    const mapped = mapResponseToApplicationError(400, {
      title: "Validation failed",
      detail: "One or more validation errors occurred.",
      errors: { Text: ["'Text' must not be empty."] },
      correlationId: "abc123",
    });

    expect(mapped).toBeInstanceOf(ValidationError);
    const validation = mapped as ValidationError;
    expect(validation.errors["Text"]).toEqual(["'Text' must not be empty."]);
    expect(validation.correlationId).toBe("abc123");
    expect(validation.message).toBe("One or more validation errors occurred.");
  });

  it("prefers the ProblemDetails detail over its title", () => {
    const mapped = mapResponseToApplicationError(422, {
      title: "Business rule violated",
      detail: "Cannot delete the last administrator.",
    });

    expect(mapped.message).toBe("Cannot delete the last administrator.");
  });

  it("stays usable when the body is not ProblemDetails, e.g. a proxy error page", () => {
    const mapped = mapResponseToApplicationError(502, "<html>Bad gateway</html>");

    expect(mapped).toBeInstanceOf(ServerError);
    expect(mapped.message).toContain("502");
  });

  it("falls back to a generic ApplicationError for unmapped 4xx statuses", () => {
    expect(mapResponseToApplicationError(418, {})).toBeInstanceOf(ApplicationError);
  });
});

describe("mapTransportFailure", () => {
  it("reports a failed request as a NetworkError", () => {
    const mapped = mapTransportFailure(new TypeError("Failed to fetch"));

    expect(mapped).toBeInstanceOf(NetworkError);
    expect(mapped.message).toContain("connection");
    expect(mapped.cause).toBeInstanceOf(TypeError);
  });

  it("reports a timeout with a retry hint", () => {
    const mapped = mapTransportFailure(new DOMException("timeout", "TimeoutError"));

    expect(mapped).toBeInstanceOf(NetworkError);
    expect(mapped.message).toContain("timed out");
  });

  it("returns an already-mapped error untouched", () => {
    const original = new ForbiddenError("nope");

    expect(mapTransportFailure(original)).toBe(original);
  });
});
