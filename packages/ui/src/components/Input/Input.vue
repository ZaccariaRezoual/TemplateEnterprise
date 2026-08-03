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
import { computed, ref, useId } from "vue";
import { cn } from "../../utils/cn";
import type { InputProps } from "./Input.types";

const props = withDefaults(defineProps<InputProps>(), {
  type: "text",
  labelHidden: false,
  required: false,
  disabled: false,
  readonly: false,
});

/** Field value, exposed through `v-model`. Always a string — see below. */
const model = defineModel<string>({ default: "" });

/**
 * True while an IME composition is in progress (Japanese, Chinese, Korean,
 * and accent composition on some layouts).
 *
 * `v-model` handles this for free; this component cannot use `v-model` (see
 * `onInput`), so it has to guard the same case itself. Writing the model
 * mid-composition makes half-typed characters visible and can cancel the
 * composition outright.
 */
const isComposing = ref(false);

/**
 * Writes the RAW string into the model.
 *
 * The reason this is not `v-model`: Vue's own directive coerces the value to
 * a NUMBER whenever the element is `type="number"`. The component would then
 * hand consumers a number while its declared contract — and its `.types.ts` —
 * promise a string, and the mismatch only shows up at runtime, in whichever
 * page first calls `.trim()` on it.
 *
 * Keeping the model a string is the honest contract: a field is text, and the
 * page that wants a number converts it deliberately.
 */
function onInput(event: Event): void {
  if (isComposing.value) {
    return;
  }
  model.value = (event.target as HTMLInputElement).value;
}

function onCompositionEnd(event: CompositionEvent): void {
  isComposing.value = false;
  model.value = (event.target as HTMLInputElement).value;
}

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
    "w-full rounded-(--input-radius) border bg-surface px-3 py-2 text-sm text-text transition-colors",
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
        :value="model"
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
        @input="onInput"
        @compositionstart="isComposing = true"
        @compositionend="onCompositionEnd"
      />
      <slot name="suffix" />
    </div>

    <p v-if="showHint" :id="hintId" class="mt-1.5 text-sm text-text-muted">{{ hint }}</p>
    <p v-if="hasError" :id="errorId" role="alert" class="mt-1.5 text-sm text-danger">
      {{ error }}
    </p>
  </div>
</template>
