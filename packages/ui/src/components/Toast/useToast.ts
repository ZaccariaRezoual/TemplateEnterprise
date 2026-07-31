import { readonly, ref, type DeepReadonly, type Ref } from "vue";
import type { IToast, IToastOptions } from "./Toast.types";

/** Module-scoped so every caller feeds the same stack, wherever it is mounted. */
const toasts = ref<IToast[]>([]);

const DEFAULT_DURATION = 5_000;

/**
 * Transient feedback messages.
 *
 * Use it for things the user does not need to act on ("Saved", "Connection
 * restored"). Anything requiring a decision belongs in a Dialog: a toast can
 * be missed, and a message that must not be missed cannot be a toast.
 *
 * The stack lives in module scope, so a feature can raise a toast without
 * knowing where `ToastHost` is mounted.
 *
 * @returns The current toasts and the functions to add or remove them.
 */
export function useToast(): {
  toasts: DeepReadonly<Ref<IToast[]>>;
  show: (options: IToastOptions) => string;
  dismiss: (id: string) => void;
  clear: () => void;
} {
  /**
   * Queues a toast.
   *
   * @param options What to show.
   * @returns The toast id, for dismissing it early.
   */
  function show(options: IToastOptions): string {
    const variant = options.variant ?? "info";
    const id = crypto.randomUUID();

    const toast: IToast = {
      id,
      title: options.title,
      variant,
      // Errors stay until dismissed: an error nobody read is reported as
      // "nothing happened".
      duration: options.duration ?? (variant === "danger" ? 0 : DEFAULT_DURATION),
      ...(options.description === undefined ? {} : { description: options.description }),
    };

    toasts.value = [...toasts.value, toast];

    if (toast.duration > 0) {
      globalThis.setTimeout(() => dismiss(id), toast.duration);
    }

    return id;
  }

  /**
   * Removes a toast.
   *
   * @param id Identifier returned by {@link show}.
   */
  function dismiss(id: string): void {
    toasts.value = toasts.value.filter((toast) => toast.id !== id);
  }

  /** Removes every toast (e.g. on sign-out or route change). */
  function clear(): void {
    toasts.value = [];
  }

  return { toasts: readonly(toasts), show, dismiss, clear };
}
