/**
 * Public contract of {@link Dialog}.
 *
 * Optional props explicitly accept `undefined` so binding a possibly-absent
 * value type-checks under `exactOptionalPropertyTypes`.
 */

/** Props accepted by {@link Dialog}. */
export interface DialogProps {
  /**
   * Accessible title of the dialog. Required: a modal without a name is
   * unusable with a screen reader.
   */
  title: string;
  /** Supporting text describing the consequence of the action. */
  description?: string | undefined;
  /**
   * Prevents closing via Escape or a backdrop click. Reserve it for
   * operations that must not be interrupted (a running migration, say) — never
   * to force a decision, which traps the user.
   */
  persistent?: boolean | undefined;
}

/** Events emitted by {@link Dialog}. */
export interface DialogEmits {
  /** Fired whenever the dialog closes, by any means. */
  close: [];
}

/** Slots of {@link Dialog}. */
export interface DialogSlots {
  /** Dialog body. */
  default: () => unknown;
  /** Footer actions, typically confirm and cancel buttons. */
  footer?: () => unknown;
}
