/**
 * Public contract of {@link PageSection}.
 *
 * Optional props explicitly accept `undefined` so binding a possibly-absent
 * computed value type-checks under `exactOptionalPropertyTypes`.
 */

/** Heading element a section may render. */
export type PageSectionHeadingLevel = "h1" | "h2" | "h3";

/** Props accepted by {@link PageSection}. */
export interface PageSectionProps {
  /** Section heading. Omit for a band of pure content. */
  title?: string | undefined;
  /** Sentence under the heading. */
  intro?: string | undefined;
  /**
   * Heading level, so the document outline stays correct.
   *
   * It is a prop because the outline of a page is not a visual choice: a
   * section under an `h1` needs an `h2`, and a component that always emits
   * `h2` quietly breaks the outline. The SIZE comes from the type-role token,
   * never from the level.
   */
  headingLevel?: PageSectionHeadingLevel | undefined;
}

/** Slots of {@link PageSection}. */
export interface PageSectionSlots {
  /** Content of the band. */
  default?: () => unknown;
}
