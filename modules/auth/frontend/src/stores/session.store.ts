import { defineStore } from "pinia";
import { computed, ref } from "vue";
import type { AuthApi, AuthResponse, AuthUser } from "../api/auth.api";

let configuredApi: AuthApi | undefined;

/**
 * Injects the module's API service. Called once by `installAuthModule` before
 * the store is first used; stores cannot take constructor arguments, so the
 * dependency arrives through this seam.
 *
 * @param api The configured auth service.
 */
export function provideAuthApi(api: AuthApi): void {
  configuredApi = api;
}

function requireApi(): AuthApi {
  if (configuredApi === undefined) {
    throw new Error("Auth module is not installed. Call installAuthModule() at bootstrap.");
  }
  return configuredApi;
}

/**
 * Session state of the application.
 *
 * Security model:
 * - The ACCESS TOKEN lives only in this store's memory — never in
 *   localStorage, where any injected script could read it.
 * - The REFRESH TOKEN lives only in an httpOnly cookie the browser manages.
 * - `restore()` runs once at startup: it exchanges the cookie for a fresh
 *   access token, which is how a session survives a page reload despite the
 *   memory-only access token.
 *
 * This is CLIENT state (who am I, right now, in this tab), so Pinia is its
 * home; the profile page's server data still flows through TanStack Query.
 */
export const useSessionStore = defineStore("auth-session", () => {
  const user = ref<AuthUser>();
  const accessToken = ref<string>();

  /** Resolves when the startup session restore has finished (either way). */
  const isRestored = ref(false);

  /** Whether a signed-in session is active. */
  const isAuthenticated = computed(() => accessToken.value !== undefined);

  function applySession(session: AuthResponse): void {
    accessToken.value = session.accessToken;
    user.value = session.user;
  }

  function clearSession(): void {
    accessToken.value = undefined;
    user.value = undefined;
  }

  /**
   * Creates an account and signs in.
   *
   * @param input Email, display name and password.
   */
  async function register(input: {
    email: string;
    displayName: string;
    password: string;
  }): Promise<void> {
    applySession(await requireApi().register(input));
  }

  /**
   * Signs in with credentials.
   *
   * @param input Email and password.
   */
  async function login(input: { email: string; password: string }): Promise<void> {
    applySession(await requireApi().login(input));
  }

  /**
   * Signs out: revokes the refresh token server-side and clears local state.
   * Local state clears even if the server call fails — the user asked to
   * leave, and a dead network must not trap them in a session.
   */
  async function logout(): Promise<void> {
    try {
      await requireApi().logout();
    } finally {
      clearSession();
    }
  }

  /**
   * Exchanges the refresh cookie for a new access token.
   * Registered as the core HTTP layer's unauthorized handler, which retries
   * the failed call once when this succeeds.
   *
   * @returns Whether the session was recovered.
   */
  async function tryRefresh(): Promise<boolean> {
    try {
      applySession(await requireApi().refresh());
      return true;
    } catch {
      clearSession();
      return false;
    }
  }

  /**
   * Startup session restore. Awaited by the router guard so protected routes
   * never flash a redirect while a valid cookie is being exchanged.
   */
  async function restore(): Promise<void> {
    if (isRestored.value) {
      return;
    }
    await tryRefresh();
    isRestored.value = true;
  }

  /**
   * Fetches the caller's profile from the API (Bearer-protected `/me`).
   * Exists on the store so pages never touch the module's API service.
   *
   * @returns The fresh profile; also refreshes `user`.
   */
  async function fetchProfile(): Promise<AuthUser> {
    const profile = await requireApi().me();
    user.value = profile;
    return profile;
  }

  return {
    user,
    accessToken,
    isAuthenticated,
    isRestored,
    register,
    login,
    logout,
    tryRefresh,
    restore,
    fetchProfile,
  };
});
