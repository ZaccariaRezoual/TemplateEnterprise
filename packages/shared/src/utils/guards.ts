/**
 * Type guards used across the codebase.
 *
 * They exist so narrowing is expressed once and consistently: `Boolean` as a
 * filter callback does not narrow types in TypeScript, and `!= null` reads
 * ambiguously to reviewers.
 */

/**
 * Narrows out `null` and `undefined`.
 *
 * @param value Value to test.
 * @returns `true` when the value is neither `null` nor `undefined`.
 *
 * @example
 * ```ts
 * const names: string[] = maybeNames.filter(isDefined);
 * ```
 */
export function isDefined<T>(value: T | null | undefined): value is T {
  return value !== null && value !== undefined;
}

/**
 * Tells whether a string contains something other than whitespace.
 *
 * @param value Value to test.
 * @returns `true` when the value is a non-blank string.
 */
export function isNonEmptyString(value: unknown): value is string {
  return typeof value === "string" && value.trim().length > 0;
}
