import { AxiosError, AxiosHeaders } from "axios";
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
import { mapToApplicationError } from "@/core/http/errorMapper";

function axiosErrorWithStatus(status: number, data: unknown = {}): AxiosError {
  const config = { headers: new AxiosHeaders() };
  return new AxiosError("Request failed", "ERR_BAD_RESPONSE", config, undefined, {
    status,
    statusText: "",
    headers: {},
    config,
    data,
  });
}

describe("mapToApplicationError", () => {
  it.each([
    [401, UnauthorizedError],
    [403, ForbiddenError],
    [404, NotFoundError],
    [422, BusinessError],
    [500, ServerError],
  ])("maps HTTP %i to the matching error type", (status, expected) => {
    const mapped = mapToApplicationError(axiosErrorWithStatus(status));

    expect(mapped).toBeInstanceOf(expected);
  });

  it("maps 400 to a ValidationError carrying the field errors", () => {
    const mapped = mapToApplicationError(
      axiosErrorWithStatus(400, {
        title: "Validation failed",
        detail: "One or more validation errors occurred.",
        errors: { Text: ["'Text' must not be empty."] },
        correlationId: "abc123",
      }),
    );

    expect(mapped).toBeInstanceOf(ValidationError);
    const validation = mapped as ValidationError;
    expect(validation.errors["Text"]).toEqual(["'Text' must not be empty."]);
    expect(validation.correlationId).toBe("abc123");
    expect(validation.message).toBe("One or more validation errors occurred.");
  });

  it("maps a response-less failure to a NetworkError", () => {
    const config = { headers: new AxiosHeaders() };
    const mapped = mapToApplicationError(
      new AxiosError("Network Error", "ERR_NETWORK", config, {}),
    );

    expect(mapped).toBeInstanceOf(NetworkError);
  });

  it("reports a timeout as a NetworkError with a retry hint", () => {
    const config = { headers: new AxiosHeaders() };
    const mapped = mapToApplicationError(
      new AxiosError("timeout exceeded", "ECONNABORTED", config, {}),
    );

    expect(mapped).toBeInstanceOf(NetworkError);
    expect(mapped.message).toContain("timed out");
  });

  it("wraps non-Axios errors so callers always receive an ApplicationError", () => {
    const mapped = mapToApplicationError(new Error("boom"));

    expect(mapped).toBeInstanceOf(ApplicationError);
    expect(mapped.cause).toBeInstanceOf(Error);
  });

  it("returns an already-mapped error untouched", () => {
    const original = new ForbiddenError("nope");

    expect(mapToApplicationError(original)).toBe(original);
  });

  it("falls back to the Axios message when the body is not ProblemDetails", () => {
    const mapped = mapToApplicationError(axiosErrorWithStatus(503, "<html>Gateway</html>"));

    expect(mapped).toBeInstanceOf(ServerError);
    expect(mapped.message).toBe("Request failed");
  });
});
