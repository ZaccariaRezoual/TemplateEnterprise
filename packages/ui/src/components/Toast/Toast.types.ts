/** Severity of a toast, mapped to semantic colour tokens. */
export type ToastVariant = "info" | "success" | "warning" | "danger";

/** A queued toast. */
export interface IToast {
  /** Stable identifier, used as the list key and to dismiss programmatically. */
  id: string;
  /** Short headline. */
  title: string;
  /** Optional explanatory line. */
  description?: string;
  /** Severity used for styling and for the accessible role. */
  variant: ToastVariant;
  /**
   * Milliseconds before auto-dismissal. `0` keeps it until dismissed —
   * required for anything a user must acknowledge.
   */
  duration: number;
}

/** Options accepted when showing a toast. */
export interface IToastOptions {
  /** Short headline. */
  title: string;
  /** Optional explanatory line. */
  description?: string;
  /** Severity; defaults to "info". */
  variant?: ToastVariant;
  /**
   * Milliseconds before auto-dismissal; defaults to 5000, or 0 (never) for
   * the "danger" variant, because an error the user did not read is an error
   * they will report as "nothing happened".
   */
  duration?: number;
}

/** Props of `ToastHost`. */
export interface IToastHostProps {
  /**
   * Maximum toasts shown at once; older ones are dropped. A stack that grows
   * without bound covers the UI it is describing.
   */
  max?: number;
}
