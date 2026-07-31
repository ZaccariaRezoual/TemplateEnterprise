import type { ApiClient, components } from "@enterprise/sdk";
import { executeSdkCall } from "@enterprise/shared";

/** A user profile as returned by the API. */
export type UserProfile = components["schemas"]["UserProfileDto"];

/** A page of user profiles. */
export type UserProfilePage = components["schemas"]["PagedResultOfUserProfileDto"];

let configuredApi: ApiClient | undefined;

/**
 * Injects the application's SDK client. Called once by `installUsersModule`.
 *
 * @param api The configured SDK client.
 */
export function provideUsersApi(api: ApiClient): void {
  configuredApi = api;
}

function requireApi(): ApiClient {
  if (configuredApi === undefined) {
    throw new Error("Users module is not installed. Call installUsersModule() at bootstrap.");
  }
  return configuredApi;
}

/**
 * Feature service of the Users module: the only place that knows which API
 * operations it uses. Components go through composables, never here.
 */
export const usersApi = {
  /**
   * Lists user profiles.
   *
   * @param params Paging and search parameters.
   * @param signal Abort signal forwarded by TanStack Query on cancellation.
   * @returns The requested page.
   * @throws {import("@enterprise/shared").ForbiddenError} Without the users.read permission.
   */
  list(
    params: { page?: number; pageSize?: number; search?: string },
    signal?: AbortSignal,
  ): Promise<UserProfilePage> {
    return executeSdkCall(() =>
      requireApi().GET("/api/users", {
        params: { query: params },
        ...(signal === undefined ? {} : { signal }),
      }),
    );
  },
};
