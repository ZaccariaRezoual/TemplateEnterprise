/**
 * Where a signed-in user belongs.
 *
 * Used after sign-in and when an authenticated user opens /login or
 * /register — pages that have no meaning inside a session. The value is set
 * once by the host at install time: an application with a public site sends
 * people to its private area, one without sends them to the root, and only
 * the host knows which it is.
 */
let homePath = "/";

/**
 * Sets the destination of an authenticated user. Called by
 * `installAuthModule` when the host provides `homePath`.
 *
 * @param path Absolute route path.
 */
export function setHomePath(path: string): void {
  homePath = path;
}

/**
 * Returns the destination of an authenticated user.
 *
 * @returns The configured path, or "/" when the host did not set one.
 */
export function getHomePath(): string {
  return homePath;
}
