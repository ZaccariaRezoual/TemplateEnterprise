/**
 * Public contract of {@link Textarea}.
 *
 * Optional props explicitly accept `undefined`: consumers bind computed values
 * that are legitimately absent (`:error="maybeError"`), and under
 * `exactOptionalPropertyTypes` a bare `?` would reject exactly that.
 */

/** Props accepted by {@link Textarea}. */
export interface TextareaProps {
  /**
   * Visible label. Required by design, for the same reason as {@link Input}:
   * a placeholder disappears on focus and is ignored by many assistive
   * technologies. Use `labelHidden` when the design calls for none.
   */
  label: string;
  /** Hides the label visually while keeping it available to screen readers. */
  labelHidden?: boolean | undefined;
  /** Short example of the expected value. Never a substitute for the label. */
  placeholder?: string | undefined;
  /** Helper text shown under the field. Hidden while an error is displayed. */
  hint?: string | undefined;
  /**
   * Validation message. When set, the field is marked invalid and the message
   * is announced to assistive technology.
   */
  error?: string | undefined;
  /** Marks the field as required, visually and for assistive technology. */
  required?: boolean | undefined;
  /** Whether the field is disabled. */
  disabled?: boolean | undefined;
  /** Whether the field is read-only. */
  readonly?: boolean | undefined;
  /**
   * Visible rows. Defaults to 5 — enough that a visitor can see a short
   * message whole, which is the difference between writing one and giving up.
   */
  rows?: number | undefined;
  /**
   * Maximum number of characters. When set, a live counter is shown: a limit
   * discovered only on submit means rewriting a message already finished.
   */
  maxlength?: number | undefined;
  /** Identifier of the field. Generated when omitted. */
  id?: string | undefined;
}
