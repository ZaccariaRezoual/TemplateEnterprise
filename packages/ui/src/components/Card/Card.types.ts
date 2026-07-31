/**
 * Public contract of {@link Card}.
 *
 * Optional props explicitly accept `undefined` so binding a possibly-absent
 * value type-checks under `exactOptionalPropertyTypes`.
 */

/** Props accepted by {@link Card}. */
export interface CardProps {
  /**
   * Heading rendered in the card header. Omit it and use the `header` slot for
   * anything richer than text.
   */
  title?: string | undefined;
  /** Supporting text under the title. */
  description?: string | undefined;
  /** Removes the inner padding, for cards whose content manages its own (tables, media). */
  flush?: boolean | undefined;
  /**
   * Heading level used for `title`, so cards nest correctly in the document
   * outline. Defaults to `h3`.
   */
  headingLevel?: "h2" | "h3" | "h4" | undefined;
}

/** Slots of {@link Card}. */
export interface CardSlots {
  /** Card body. */
  default: () => unknown;
  /** Replaces the title/description block. */
  header?: () => unknown;
  /** Actions aligned to the right of the header, e.g. a menu button. */
  actions?: () => unknown;
  /** Footer area, typically holding the card's primary action. */
  footer?: () => unknown;
}
