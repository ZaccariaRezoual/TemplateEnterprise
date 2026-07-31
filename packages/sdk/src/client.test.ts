import { describe, expect, it, vi } from "vitest";
import { createApiClient } from "./client";

/**
 * These tests guard the SDK's contract with the rest of the framework: that
 * the generated schema really covers the API surface, and that the client
 * routes calls through the injected transport rather than the global fetch —
 * which is what lets the application attach correlation ids, auth tokens and
 * error mapping to every request.
 */
describe("createApiClient", () => {
  function jsonResponse(body: unknown, status = 200): Response {
    return new Response(JSON.stringify(body), {
      status,
      headers: { "Content-Type": "application/json" },
    });
  }

  /** The Request openapi-fetch handed to the transport on its first call. */
  function firstRequest(mock: ReturnType<typeof vi.fn>): Request {
    return mock.mock.calls[0]?.[0] as Request;
  }

  it("issues requests through the injected fetch, not the global one", async () => {
    const fetchMock = vi.fn(async () => jsonResponse({ message: "pong", timestampUtc: "" }));
    const api = createApiClient({ baseUrl: "https://api.test", fetch: fetchMock });

    await api.GET("/api/demo/ping");

    expect(fetchMock).toHaveBeenCalledTimes(1);
    const request = firstRequest(fetchMock);
    expect(request.url).toContain("/api/demo/ping");
  });

  it("prefixes paths with the configured base URL", async () => {
    const fetchMock = vi.fn(async () => jsonResponse({ message: "pong", timestampUtc: "" }));
    const api = createApiClient({ baseUrl: "https://api.test", fetch: fetchMock });

    await api.GET("/api/demo/ping");

    const request = firstRequest(fetchMock);
    expect(request.url).toBe("https://api.test/api/demo/ping");
  });

  it("returns the typed payload of a successful response", async () => {
    const fetchMock = vi.fn(async () =>
      jsonResponse({ message: "pong", timestampUtc: "2026-07-31T10:00:00Z" }),
    );
    const api = createApiClient({ baseUrl: "https://api.test", fetch: fetchMock });

    const { data, error } = await api.GET("/api/demo/ping");

    expect(error).toBeUndefined();
    expect(data?.message).toBe("pong");
  });

  it("reports a failure as a value instead of throwing, carrying the ProblemDetails body", async () => {
    const problem = {
      title: "Validation failed",
      errors: { Text: ["'Text' must not be empty."] },
    };
    const fetchMock = vi.fn(async () => jsonResponse(problem, 400));
    const api = createApiClient({ baseUrl: "https://api.test", fetch: fetchMock });

    const { data, error, response } = await api.POST("/api/demo/echo", { body: { text: "" } });

    expect(data).toBeUndefined();
    expect(response.status).toBe(400);
    expect(error).toMatchObject(problem);
  });

  it("sends the request body as JSON", async () => {
    const fetchMock = vi.fn(async () => jsonResponse({ text: "hi", length: 2 }));
    const api = createApiClient({ baseUrl: "https://api.test", fetch: fetchMock });

    await api.POST("/api/demo/echo", { body: { text: "hi" } });

    const request = firstRequest(fetchMock);
    expect(request.method).toBe("POST");
    await expect(request.json()).resolves.toEqual({ text: "hi" });
  });

  it("applies middleware to every request, which is how cross-cutting headers are attached", async () => {
    const fetchMock = vi.fn(async () => jsonResponse({ message: "pong", timestampUtc: "" }));
    const api = createApiClient({ baseUrl: "https://api.test", fetch: fetchMock });
    api.use({
      onRequest({ request }) {
        request.headers.set("X-Correlation-ID", "test-correlation-id");
        return request;
      },
    });

    await api.GET("/api/demo/ping");

    const request = firstRequest(fetchMock);
    expect(request.headers.get("X-Correlation-ID")).toBe("test-correlation-id");
  });
});
