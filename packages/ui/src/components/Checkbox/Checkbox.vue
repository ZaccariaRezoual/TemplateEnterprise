<script setup lang="ts">
/**
 * -----------------------------------------------------------------------------
 * Checkbox
 * -----------------------------------------------------------------------------
 *
 * The boolean primitive of the design system: one independent yes/no answer.
 *
 * Use it when the two states are "on" and "off" and the change takes effect
 * when the form is saved. For a setting that applies the moment it is
 * flipped, a switch is the honest control — the design system does not ship
 * one yet, and adding it here would let the two be used interchangeably,
 * which is exactly the confusion a switch is supposed to remove.
 *
 * Responsibilities:
 * - Renders a native `<input type="checkbox">`, so keyboard, form
 *   participation and assistive technology work without re-implementation.
 * - Wires the accessibility relationships that are invisible when missing:
 *   `for`/`id`, `aria-describedby`, `aria-invalid`, `role="alert"` on the
 *   error.
 * - Exposes the state through `v-model`.
 *
 * The touch target is the whole label row, not the 16px box: on a phone the
 * box alone is well under the 44px the tokens require.
 */
import { computed, useId } from "vue";
import { cn } from "../../utils/cn";
import type { CheckboxProps } from "./Checkbox.types";

const props = withDefaults(defineProps<CheckboxProps>(), {
  disabled: false,
});

/** Checked state, exposed through `v-model`. */
const model = defineModel<boolean>({ default: false });

// useId gives a stable id across server render and hydration, which a
// module-level counter cannot guarantee.
const generatedId = useId();
const inputId = computed(() => props.id ?? generatedId);
const hintId = computed(() => `${inputId.value}-hint`);
const errorId = computed(() => `${inputId.value}-error`);

const hasError = computed(() => props.error !== undefined && props.error.length > 0);
const showHint = computed(() => !hasError.value && props.hint !== undefined && props.hint !== "");

/** The error replaces the hint as description, so only one is referenced. */
const describedBy = computed(() => {
  if (hasError.value) {
    return errorId.value;
  }
  return showHint.value ? hintId.value : undefined;
});
</script>

<template>
  <div class="w-full">
    <!-- The label wraps the box so the whole row is the hit area. -->
    <label
      :for="inputId"
      :class="
        cn(
          'flex cursor-pointer items-start gap-3 py-1.5',
          disabled && 'cursor-not-allowed opacity-(--opacity-disabled)',
        )
      "
    >
      <input
        :id="inputId"
        v-model="model"
        type="checkbox"
        :disabled="disabled"
        :aria-invalid="hasError ? 'true' : undefined"
        :aria-describedby="describedBy"
        :class="
          cn(
            'mt-0.5 size-4 shrink-0 cursor-pointer rounded-(--checkbox-radius) border bg-surface',
            'accent-primary transition-colors disabled:cursor-not-allowed',
            hasError ? 'border-danger' : 'border-border',
          )
        "
      />
      <span class="text-sm font-medium text-text">{{ label }}</span>
    </label>

    <p v-if="showHint" :id="hintId" class="ml-7 text-sm text-text-muted">{{ hint }}</p>
    <p v-if="hasError" :id="errorId" role="alert" class="ml-7 text-sm text-danger">
      {{ error }}
    </p>
  </div>
</template>
