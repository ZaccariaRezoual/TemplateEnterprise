import { clsx, type ClassValue } from "clsx";
import { twMerge } from "tailwind-merge";

/**
 * Merges Tailwind class lists, letting the last conflicting utility win.
 *
 * Why it is needed: Vue's attribute fallthrough concatenates a consumer's
 * `class` with the component's own, so `bg-primary` and a consumer's
 * `bg-surface` would both be emitted and the winner would depend on CSS
 * source order — invisible, fragile behavior. `twMerge` resolves the conflict
 * deterministically in favor of the consumer.
 *
 * @param inputs Class values (strings, arrays, conditional objects).
 * @returns The merged class string.
 *
 * @example
 * ```ts
 * cn("px-4 py-2 bg-primary", props.class) // consumer's bg-* wins
 * ```
 */
export function cn(...inputs: ClassValue[]): string {
  return twMerge(clsx(inputs));
}
