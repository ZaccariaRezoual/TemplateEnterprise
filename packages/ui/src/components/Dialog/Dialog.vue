<script setup lang="ts">
/**
 * -----------------------------------------------------------------------------
 * Dialog
 * -----------------------------------------------------------------------------
 *
 * Modal dialog, open state controlled through `v-model:open`.
 *
 * Built on the native `<dialog>` element with `showModal()`, which the browser
 * already implements correctly: focus is trapped inside, the rest of the page
 * is inert, Escape closes, and focus returns to the trigger on close. A
 * hand-rolled div-based modal has to reimplement all of that and usually gets
 * the focus return wrong.
 *
 * Responsibilities:
 * - Opens and closes the native dialog in sync with the model.
 * - Labels the dialog with its title and description.
 * - Closes on backdrop click unless `persistent`.
 */
import { onBeforeUnmount, ref, useId, watch } from "vue";
import type { DialogEmits, DialogProps } from "./Dialog.types";

const props = withDefaults(defineProps<DialogProps>(), { persistent: false });
const emit = defineEmits<DialogEmits>();

/** Whether the dialog is open. */
const open = defineModel<boolean>("open", { default: false });

const dialogRef = ref<HTMLDialogElement>();
const titleId = useId();
const descriptionId = useId();

watch(
  [open, dialogRef],
  ([isOpen, element]) => {
    if (element === undefined) {
      return;
    }
    if (isOpen && !element.open) {
      element.showModal();
    } else if (!isOpen && element.open) {
      element.close();
    }
  },
  { immediate: true },
);

/** Fired by the browser on Escape as well as on programmatic close. */
function onNativeClose(): void {
  open.value = false;
  emit("close");
}

/** Escape is the browser's own affordance; suppress it only when persistent. */
function onCancel(event: Event): void {
  if (props.persistent) {
    event.preventDefault();
  }
}

/**
 * Closes on backdrop click. The native dialog reports clicks on its backdrop
 * as clicks on the element itself, so the check is "did the click land outside
 * the content box".
 */
function onBackdropClick(event: MouseEvent): void {
  if (props.persistent || event.target !== dialogRef.value) {
    return;
  }

  const bounds = dialogRef.value.getBoundingClientRect();
  const isOutside =
    event.clientX < bounds.left ||
    event.clientX > bounds.right ||
    event.clientY < bounds.top ||
    event.clientY > bounds.bottom;

  if (isOutside) {
    open.value = false;
  }
}

// A dialog unmounted while open would otherwise leave the page inert.
onBeforeUnmount(() => {
  if (dialogRef.value?.open === true) {
    dialogRef.value.close();
  }
});
</script>

<template>
  <dialog
    ref="dialogRef"
    :aria-labelledby="titleId"
    :aria-describedby="description ? descriptionId : undefined"
    class="m-auto w-[min(32rem,calc(100vw-2rem))] rounded-(--dialog-radius) border border-border bg-surface p-0 text-text backdrop:bg-black/50"
    @close="onNativeClose"
    @cancel="onCancel"
    @click="onBackdropClick"
  >
    <div class="p-5">
      <h2 :id="titleId" class="text-lg font-semibold tracking-tight">{{ title }}</h2>
      <p v-if="description" :id="descriptionId" class="mt-1 text-sm text-text-muted">
        {{ description }}
      </p>

      <div class="mt-4">
        <slot />
      </div>
    </div>

    <div v-if="$slots.footer" class="flex justify-end gap-2 border-t border-border px-5 py-4">
      <slot name="footer" />
    </div>
  </dialog>
</template>
