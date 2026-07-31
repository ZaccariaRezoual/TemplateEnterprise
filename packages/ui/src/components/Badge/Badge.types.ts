/**
 * Public contract of {@link Badge}.
 *
 * Optional props explicitly accept `undefined` so binding a possibly-absent
 * value type-checks under `exactOptionalPropertyTypes`.
 */

/**
 * Meaning conveyed by the badge. Choose by MEANING, not by color: the palette
 * changes per theme and per brand, the semantics do not.
 */
export type BadgeVariant = "neutral" | "success" | "warning" | "danger" | "info";

/** Props accepted by {@link Badge}. */
export interface BadgeProps {
  /** Semantic variant. Defaults to `neutral`. */
  variant?: BadgeVariant | undefined;
  /**
   * Text announced to assistive technology in place of the visible label.
   * Use it when color or a short label carries meaning that is not spelled
   * out, e.g. "Active" rendered as a dot.
   */
  srLabel?: string | undefined;
}

/** Slots of {@link Badge}. */
export interface BadgeSlots {
  /** Badge label. Keep it to one or two words. */
  default: () => unknown;
}
