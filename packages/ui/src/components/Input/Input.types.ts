/**
 * Public contract of {@link Input}.
 *
 * Optional props explicitly accept `undefined`: consumers bind computed values
 * that are legitimately absent (`:error="maybeError"`), and under
 * `exactOptionalPropertyTypes` a bare `?` would reject exactly that — the most
 * common way these components are used.
 */

/** Native input types this component supports. */
export type InputType = "text" | "email" | "password" | "search" | "tel" | "url" | "number";

/** Props accepted by {@link Input}. */
export interface InputProps {
  /**
   * Visible label. Required by design: a placeholder is not a label — it
   * disappears on focus and is ignored by many assistive technologies. Use
   * `labelHidden` when the design calls for no visible label.
   */
  label: string;
  /** Hides the label visually while keeping it available to screen readers. */
  labelHidden?: boolean | undefined;
  /** Native input type. Defaults to `text`. */
  type?: InputType | undefined;
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
  /** Native autocomplete hint. */
  autocomplete?: string | undefined;
  /** Identifier of the input. Generated when omitted. */
  id?: string | undefined;
}

/** Slots of {@link Input}. */
export interface InputSlots {
  /** Content rendered before the input, e.g. a currency symbol or an icon. */
  prefix?: () => unknown;
  /** Content rendered after the input, e.g. a unit or a reveal toggle. */
  suffix?: () => unknown;
}
