import type { ApiClient, components } from "@enterprise/sdk";
import { executeSdkCall } from "@enterprise/shared";

/** Session payload returned by register, login and refresh. */
export type AuthResponse = components["schemas"]["AuthResponse"];

/** Account profile as exposed by the API. */
export type AuthUser = components["schemas"]["UserDto"];

/**
 * Feature service of the Auth module: the only place in the module that knows
 * the auth operations.
 *
 * It executes SDK calls DIRECTLY (via the shared unwrap), not through the
 * app's retrying request wrapper: a 401 from these endpoints means the
 * session itself is dead, and retry-after-refresh would recurse into refresh.
 * The refresh token never appears here — it lives in an httpOnly cookie the
 * browser attaches by itself.
 */
export class AuthApi {
  readonly #api: ApiClient;

  /**
   * @param api The application's configured SDK client (injected so every
   * call keeps the app's middleware: correlation id, bearer token).
   */
  constructor(api: ApiClient) {
    this.#api = api;
  }

  /**
   * Creates an account and signs it in.
   *
   * @param input Email, display name and password.
   * @returns The new session.
   * @throws {import("@enterprise/shared").ValidationError} On rejected fields.
   * @throws {import("@enterprise/shared").BusinessError} When the email is taken.
   */
  register(input: { email: string; displayName: string; password: string }): Promise<AuthResponse> {
    return executeSdkCall(() => this.#api.POST("/api/auth/register", { body: input }));
  }

  /**
   * Authenticates with email and password.
   *
   * @param input Credentials.
   * @returns The session.
   * @throws {import("@enterprise/shared").UnauthorizedError} On wrong credentials.
   */
  login(input: { email: string; password: string }): Promise<AuthResponse> {
    return executeSdkCall(() => this.#api.POST("/api/auth/login", { body: input }));
  }

  /**
   * Exchanges the refresh cookie for a fresh session (rotation).
   *
   * @returns The new session.
   * @throws {import("@enterprise/shared").UnauthorizedError} When no valid session exists.
   */
  refresh(): Promise<AuthResponse> {
    return executeSdkCall(() => this.#api.POST("/api/auth/refresh"));
  }

  /**
   * Revokes the current refresh token and clears its cookie.
   */
  async logout(): Promise<void> {
    await executeSdkCall(() => this.#api.POST("/api/auth/logout"));
  }

  /**
   * Loads the authenticated caller's profile.
   *
   * @returns The profile.
   * @throws {import("@enterprise/shared").UnauthorizedError} When not signed in.
   */
  me(): Promise<AuthUser> {
    return executeSdkCall(() => this.#api.GET("/api/auth/me"));
  }
}
