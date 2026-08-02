<script setup lang="ts">
/**
 * -----------------------------------------------------------------------------
 * Textarea
 * -----------------------------------------------------------------------------
 *
 * Labelled multi-line field.
 *
 * Use it instead of {@link Input} whenever the expected answer is prose: a
 * single-line box tells the visitor "one line is enough", and they write
 * accordingly.
 *
 * Responsibilities:
 * - Same accessibility wiring as `Input` — `for`/`id`, `aria-describedby`,
 *   `aria-invalid`, `aria-required`, `role="alert"` on the error.
 * - An optional live character counter when `maxlength` is set.
 *
 * The counter is `aria-live="polite"`: a screen-reader user needs to know
 * they are near the limit, but not to hear a number after every keystroke.
 */
import { computed, useId } from "vue";
import { cn } from "../../utils/cn";
import type { TextareaProps } from "./Textarea.types";

const props = withDefaults(defineProps<TextareaProps>(), {
  labelHidden: false,
  required: false,
  disabled: false,
  readonly: false,
  rows: 5,
});

/** Field value, exposed through `v-model`. */
const model = defineModel<string>({ default: "" });

const generatedId = useId();
const fieldId = computed(() => props.id ?? generatedId);
const hintId = computed(() => `${fieldId.value}-hint`);
const errorId = computed(() => `${fieldId.value}-error`);

const hasError = computed(() => props.error !== undefined && props.error.length > 0);
const showHint = computed(() => !hasError.value && props.hint !== undefined && props.hint !== "");
const showCounter = computed(() => props.maxlength !== undefined);

/** The error replaces the hint as description, so only one is referenced. */
const describedBy = computed(() => {
  if (hasError.value) {
    return errorId.value;
  }
  return showHint.value ? hintId.value : undefined;
});

const fieldClasses = computed(() =>
  cn(
    "w-full rounded-(--input-radius) border bg-surface px-3 py-2 text-sm text-text transition-colors",
    "placeholder:text-text-muted disabled:cursor-not-allowed disabled:opacity-60",
    hasError.value ? "border-danger" : "border-border",
  ),
);
</script>

<template>
  <div class="w-full">
    <label :for="fieldId" :class="cn('block text-sm font-medium', labelHidden && 'sr-only')">
      {{ label }}
      <span v-if="required" class="text-danger" aria-hidden="true">*</span>
    </label>

    <textarea
      :id="fieldId"
      v-model="model"
      :rows="rows"
      :placeholder="placeholder"
      :disabled="disabled"
      :readonly="readonly"
      :required="required"
      :maxlength="maxlength"
      :aria-required="required ? 'true' : undefined"
      :aria-invalid="hasError ? 'true' : undefined"
      :aria-describedby="describedBy"
      :class="cn('mt-1.5', fieldClasses)"
    />

    <div class="mt-1.5 flex items-start justify-between gap-3">
      <p v-if="showHint" :id="hintId" class="text-sm text-text-muted">{{ hint }}</p>
      <p v-if="hasError" :id="errorId" role="alert" class="text-sm text-danger">{{ error }}</p>

      <p
        v-if="showCounter"
        class="ml-auto shrink-0 text-xs text-text-muted tabular-nums"
        aria-live="polite"
      >
        {{ model.length }}/{{ maxlength }}
      </p>
    </div>
  </div>
</template>
