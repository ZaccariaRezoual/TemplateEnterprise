/**
 * Public contract of {@link Skeleton}.
 *
 * Optional props explicitly accept `undefined` so binding a possibly-absent
 * value type-checks under `exactOptionalPropertyTypes`.
 */

/** Shape of the placeholder. */
export type SkeletonShape = "text" | "block" | "circle";

/** Props accepted by {@link Skeleton}. */
export interface SkeletonProps {
  /**
   * Shape of the placeholder. `text` is a single line of body height,
   * `block` fills the space it is given, `circle` is for avatars.
   * Defaults to `text`.
   */
  shape?: SkeletonShape | undefined;
  /** CSS width, e.g. `"8rem"` or `"60%"`. Defaults to filling the container. */
  width?: string | undefined;
  /** CSS height. Required in practice for `block` and `circle`. */
  height?: string | undefined;
}
