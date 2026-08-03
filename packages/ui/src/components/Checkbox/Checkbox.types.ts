/**
 * Public contract of {@link Checkbox}.
 *
 * Optional props explicitly accept `undefined`: consumers bind computed values
 * that are legitimately absent (`:error="maybeError"`), and under
 * `exactOptionalPropertyTypes` a bare `?` would reject exactly that.
 */

/** Props accepted by {@link Checkbox}. */
export interface CheckboxProps {
  /**
   * Visible label, always to the RIGHT of the box and clickable with it.
   * Required by design, like {@link Input}: a checkbox with no label is a
   * square whose meaning lives only in the developer's head.
   */
  label: string;
  /**
   * Helper text shown under the label. Use it for the consequence of ticking
   * the box, which the label rarely has room for ("visitors will see it").
   */
  hint?: string | undefined;
  /**
   * Validation message. When set, the control is marked invalid and the
   * message is announced to assistive technology.
   */
  error?: string | undefined;
  /** Whether the control is disabled. */
  disabled?: boolean | undefined;
  /** Identifier of the input. Generated when omitted. */
  id?: string | undefined;
}
