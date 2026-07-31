/**
 * Public contract of {@link Avatar}.
 *
 * Optional props explicitly accept `undefined` so binding a possibly-absent
 * value type-checks under `exactOptionalPropertyTypes`.
 */

/** Size of the avatar. */
export type AvatarSize = "sm" | "md" | "lg";

/** Props accepted by {@link Avatar}. */
export interface AvatarProps {
  /**
   * Name of the person or entity. Required: it produces the initials fallback
   * and the accessible name, so an avatar is never an unlabelled image.
   */
  name: string;
  /** Image URL. When absent or failing to load, initials are shown instead. */
  src?: string | undefined;
  /** Size. Defaults to `md`. */
  size?: AvatarSize | undefined;
  /**
   * Marks the avatar as decorative — used when the name is already displayed
   * next to it, so screen readers do not announce it twice.
   */
  decorative?: boolean | undefined;
}
