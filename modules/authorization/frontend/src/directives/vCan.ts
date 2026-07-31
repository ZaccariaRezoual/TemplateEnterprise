import type { App, Directive, DirectiveBinding } from "vue";
import { usePermissionsStore } from "../stores/permissions.store";

/**
 * `v-can` — removes an element unless the user holds the permission.
 *
 * ```vue
 * <button v-can="'users.write'">Edit</button>
 * <button v-can="['users.write', 'users.delete']">Manage</button>  <!-- any of -->
 * ```
 *
 * It REMOVES rather than hides: an element that is merely invisible is still
 * in the accessibility tree and still focusable by keyboard, which turns a
 * hidden action into a confusing one.
 *
 * Like every client-side check this is presentation only — the API enforces
 * the same permission independently.
 */
function resolve(binding: DirectiveBinding<string | readonly string[]>): boolean {
  const store = usePermissionsStore();
  const value = binding.value;
  return typeof value === "string" ? store.can(value) : store.canAny(value);
}

function apply(element: HTMLElement, binding: DirectiveBinding<string | readonly string[]>): void {
  const allowed = resolve(binding);
  // A comment node keeps the element's position, so re-granting a permission
  // restores it where it belongs instead of appending it at the end.
  const placeholder = (element as unknown as { _vCanPlaceholder?: Comment })._vCanPlaceholder;

  if (!allowed && element.parentNode !== null) {
    const marker = placeholder ?? document.createComment("v-can");
    (element as unknown as { _vCanPlaceholder?: Comment })._vCanPlaceholder = marker;
    element.parentNode.replaceChild(marker, element);
    return;
  }

  if (allowed && placeholder?.parentNode != null) {
    placeholder.parentNode.replaceChild(element, placeholder);
  }
}

/** The directive implementation. */
export const vCan: Directive<HTMLElement, string | readonly string[]> = {
  mounted: apply,
  updated: apply,
};

/**
 * Registers `v-can` on the application.
 *
 * @param app The Vue application instance.
 */
export function installCanDirective(app: App): void {
  app.directive("can", vCan);
}
