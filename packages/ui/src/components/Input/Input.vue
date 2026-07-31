<script setup lang="ts">
/**
 * -----------------------------------------------------------------------------
 * Input
 * -----------------------------------------------------------------------------
 *
 * Labelled text field: the form primitive of the design system.
 *
 * Responsibilities:
 * - Renders a label bound to the input, plus optional hint and error text.
 * - Wires the accessibility relationships that are easy to get wrong and
 *   invisible when missing: `for`/`id`, `aria-describedby`, `aria-invalid`,
 *   `aria-required`, and `role="alert"` on the error so it is announced.
 * - Exposes the value through `v-model`.
 *
 * Centralizing this wiring is the point: every form in every project inherits
 * correct semantics instead of re-implementing them per feature.
 */
import { computed, useId } from "vue";
import { cn } from "../../utils/cn";
import type { InputProps } from "./Input.types";

const props = withDefaults(defineProps<InputProps>(), {
  type: "text",
  labelHidden: false,
  required: false,
  disabled: false,
  readonly: false,
});

/** Field value, exposed through `v-model`. */
const model = defineModel<string>({ default: "" });

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

const inputClasses = computed(() =>
  cn(
    "w-full rounded-md border bg-surface px-3 py-2 text-sm text-text transition-colors",
    "placeholder:text-text-muted disabled:cursor-not-allowed disabled:opacity-60",
    hasError.value ? "border-danger" : "border-border",
  ),
);
</script>

<template>
  <div class="w-full">
    <label :for="inputId" :class="cn('block text-sm font-medium', labelHidden && 'sr-only')">
      {{ label }}
      <span v-if="required" class="text-danger" aria-hidden="true">*</span>
    </label>

    <div class="mt-1.5 flex items-center gap-2">
      <slot name="prefix" />
      <input
        :id="inputId"
        v-model="model"
        :type="type"
        :placeholder="placeholder"
        :disabled="disabled"
        :readonly="readonly"
        :autocomplete="autocomplete"
        :required="required"
        :aria-required="required ? 'true' : undefined"
        :aria-invalid="hasError ? 'true' : undefined"
        :aria-describedby="describedBy"
        :class="inputClasses"
      />
      <slot name="suffix" />
    </div>

    <p v-if="showHint" :id="hintId" class="mt-1.5 text-sm text-text-muted">{{ hint }}</p>
    <p v-if="hasError" :id="errorId" role="alert" class="mt-1.5 text-sm text-danger">
      {{ error }}
    </p>
  </div>
</template>
