/**
 * Public contract of {@link Button}.
 *
 * Consumers type against these exported types; they must stay stable, because
 * every application built on the framework depends on them. Optional props
 * explicitly accept `undefined` so binding a possibly-absent computed value
 * type-checks under `exactOptionalPropertyTypes`.
 */

/**
 * Visual intent of the button.
 *
 * - `primary`: the main action of a view. At most one per screen area.
 * - `secondary`: supporting actions of equal weight.
 * - `ghost`: low-emphasis actions inside dense UI (toolbars, table rows).
 * - `danger`: destructive actions. Always pair with a confirmation step.
 */
export type ButtonVariant = "primary" | "secondary" | "ghost" | "danger";

/** Size of the button. Use `md` unless density demands otherwise. */
export type ButtonSize = "sm" | "md" | "lg";

/** Props accepted by {@link Button}. */
export interface ButtonProps {
  /** Visual intent. Defaults to `primary`. */
  variant?: ButtonVariant | undefined;
  /** Size. Defaults to `md`. */
  size?: ButtonSize | undefined;
  /**
   * Native button type. Defaults to `button` — NOT `submit`, so a button
   * dropped into a form never submits it by accident.
   */
  type?: "button" | "submit" | "reset" | undefined;
  /** Whether the button is disabled. */
  disabled?: boolean | undefined;
  /**
   * Whether an operation triggered by this button is in flight. Shows a
   * spinner, blocks further clicks and exposes `aria-busy` to assistive tech.
   */
  loading?: boolean | undefined;
  /** Whether the button fills the width of its container. */
  block?: boolean | undefined;
}

/** Events emitted by {@link Button}. */
export interface ButtonEmits {
  /**
   * Fired on click. Not emitted while `disabled` or `loading`.
   *
   * @param event The originating pointer event.
   */
  click: [event: MouseEvent];
}

/** Slots of {@link Button}. */
export interface ButtonSlots {
  /** Button label. Keep it an action verb ("Save", not "OK"). */
  default: () => unknown;
  /** Optional icon rendered before the label. */
  icon?: () => unknown;
}
