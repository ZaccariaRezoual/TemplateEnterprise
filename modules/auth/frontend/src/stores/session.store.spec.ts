import { createPinia, setActivePinia } from "pinia";
import { beforeEach, describe, expect, it, vi } from "vitest";
import type { AuthApi, AuthResponse } from "../api/auth.api";
import { provideAuthApi, useSessionStore } from "./session.store";

/** Builds a session payload as the API would return it. */
function sessionOf(email: string, token = "jwt-1"): AuthResponse {
  return {
    accessToken: token,
    accessTokenExpiresAtUtc: new Date(Date.now() + 900_000).toISOString(),
    user: {
      id: "00000000-0000-4000-8000-000000000001",
      email,
      displayName: "Ada",
      roles: ["User"],
    },
  };
}

function mockApi(overrides: Partial<Record<keyof AuthApi, unknown>> = {}): AuthApi {
  return {
    register: vi.fn(async () => sessionOf("ada@example.com")),
    login: vi.fn(async () => sessionOf("ada@example.com")),
    refresh: vi.fn(async () => sessionOf("ada@example.com", "jwt-2")),
    logout: vi.fn(async () => undefined),
    me: vi.fn(async () => sessionOf("ada@example.com").user),
    ...overrides,
  } as AuthApi;
}

describe("useSessionStore", () => {
  beforeEach(() => {
    setActivePinia(createPinia());
  });

  it("login stores the access token in memory and exposes the user", async () => {
    provideAuthApi(mockApi());
    const session = useSessionStore();

    await session.login({ email: "ada@example.com", password: "pw" });

    expect(session.isAuthenticated).toBe(true);
    expect(session.accessToken).toBe("jwt-1");
    expect(session.user?.email).toBe("ada@example.com");
    // The token must never be persisted where scripts can read it.
    expect(globalThis.localStorage.getItem("auth-session")).toBeNull();
  });

  it("logout clears the session even when the server call fails", async () => {
    provideAuthApi(
      mockApi({
        logout: vi.fn(async () => {
          throw new Error("network down");
        }),
      }),
    );
    const session = useSessionStore();
    await session.login({ email: "ada@example.com", password: "pw" });

    await expect(session.logout()).rejects.toThrow("network down");

    expect(session.isAuthenticated).toBe(false);
    expect(session.user).toBeUndefined();
  });

  it("tryRefresh recovers the session and reports success", async () => {
    provideAuthApi(mockApi());
    const session = useSessionStore();

    await expect(session.tryRefresh()).resolves.toBe(true);
    expect(session.accessToken).toBe("jwt-2");
  });

  it("tryRefresh clears state and reports failure when the cookie is dead", async () => {
    provideAuthApi(
      mockApi({
        refresh: vi.fn(async () => {
          throw new Error("401");
        }),
      }),
    );
    const session = useSessionStore();

    await expect(session.tryRefresh()).resolves.toBe(false);
    expect(session.isAuthenticated).toBe(false);
  });

  it("restore runs the cookie exchange exactly once", async () => {
    const api = mockApi();
    provideAuthApi(api);
    const session = useSessionStore();

    await session.restore();
    await session.restore();

    expect(api.refresh).toHaveBeenCalledTimes(1);
    expect(session.isRestored).toBe(true);
  });
});
