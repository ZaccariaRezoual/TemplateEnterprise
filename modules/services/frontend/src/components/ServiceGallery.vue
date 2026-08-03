<script setup lang="ts">
/**
 * -----------------------------------------------------------------------------
 * ServiceGallery
 * -----------------------------------------------------------------------------
 *
 * The images of a service, in the administration: upload, choose the cover,
 * detach.
 *
 * Responsibilities:
 * - Shows the current gallery, marking which image is the cover.
 * - Takes a file and its text alternative and hands both to the composable,
 *   which uploads to Storage and attaches the result in one step.
 * - Refuses to upload without an alt text, before the request leaves.
 *
 * The alt text is a required field of the upload form, not an afterthought on
 * the list: an image without one is invisible to anyone who cannot see it,
 * and "we'll add it later" is how it never gets added. The design system
 * states the rule; this form is where it is actually enforced.
 *
 * It only appears on a service that already exists — an image needs something
 * to belong to — which is why the edit page hides it while creating one.
 */
import { Button, Card, Input } from "@enterprise/ui";
import { computed, ref, toRef, useTemplateRef } from "vue";
import { serviceImageUrl, type ServiceImage } from "../api/services.api";
import { useServiceImages } from "../composables/useServices";

const props = defineProps<{
  /** Service the gallery belongs to. */
  serviceId: string;
  /** Images currently attached, as the server returned them. */
  images: readonly ServiceImage[];
}>();

const { attach, setCover, detach } = useServiceImages(toRef(props, "serviceId"));

const file = ref<File | undefined>(undefined);
const altText = ref("");
const localError = ref<string | undefined>(undefined);
const fileInput = useTemplateRef<HTMLInputElement>("fileInput");

/**
 * The next free position, so a new image lands at the end of the gallery.
 *
 * `Number(...)` because the generated SDK types an int32 as `number | string`:
 * the OpenAPI document allows both, and arithmetic on the union would not
 * compile.
 */
const nextSortOrder = computed(() =>
  props.images.reduce((highest, image) => Math.max(highest, Number(image.sortOrder) + 1), 0),
);

function onFileChange(event: Event): void {
  const input = event.target as HTMLInputElement;
  file.value = input.files?.[0];
  localError.value = undefined;
}

async function upload(): Promise<void> {
  localError.value = undefined;

  if (file.value === undefined) {
    localError.value = "Scegli un'immagine.";
    return;
  }

  if (altText.value.trim() === "") {
    localError.value =
      "Descrivi l'immagine: senza testo alternativo è invisibile a chi non la vede.";
    return;
  }

  await attach.mutateAsync({
    file: file.value,
    altText: altText.value.trim(),
    sortOrder: nextSortOrder.value,
  });

  file.value = undefined;
  altText.value = "";

  // A native file input keeps its selection after a successful upload, so the
  // next click on "Carica" would send the same picture again.
  if (fileInput.value !== null) {
    fileInput.value.value = "";
  }
}
</script>

<template>
  <Card title="Immagini" heading-level="h2">
    <p v-if="images.length === 0" class="text-sm text-text-muted">
      Nessuna immagine. La prima che carichi diventa la copertina, quella che si vede in elenco e
      nelle anteprime dei link.
    </p>

    <ul v-else class="grid grid-cols-1 gap-4 sm:grid-cols-2 lg:grid-cols-3">
      <li
        v-for="image in images"
        :key="image.id"
        class="overflow-hidden rounded-(--card-radius) border border-border"
      >
        <img
          :src="serviceImageUrl(image.storageFileId)"
          :alt="image.altText"
          class="aspect-video w-full bg-surface-sunken object-cover"
          loading="lazy"
        />
        <div class="space-y-2 p-3">
          <p class="text-sm text-text-muted">{{ image.altText }}</p>
          <div class="flex flex-wrap items-center gap-2">
            <span v-if="image.isCover" class="text-sm font-medium text-primary">Copertina</span>
            <Button
              v-else
              variant="ghost"
              size="sm"
              :loading="setCover.isPending.value"
              @click="setCover.mutate(image.id)"
            >
              Usa come copertina
            </Button>
            <Button
              variant="ghost"
              size="sm"
              class="ml-auto text-danger"
              :loading="detach.isPending.value"
              @click="detach.mutate(image.id)"
            >
              Rimuovi
            </Button>
          </div>
        </div>
      </li>
    </ul>

    <div class="mt-6 space-y-3 border-t border-border pt-6">
      <div>
        <label for="service-image-file" class="block text-sm font-medium">Nuova immagine</label>
        <input
          id="service-image-file"
          ref="fileInput"
          type="file"
          accept="image/png,image/jpeg,image/webp,image/avif,image/gif"
          class="mt-1.5 block w-full text-sm text-text-muted"
          @change="onFileChange"
        />
      </div>

      <Input
        v-model="altText"
        label="Testo alternativo"
        required
        hint="Cosa mostra l'immagine, per chi non può vederla."
      />

      <Button :loading="attach.isPending.value" @click="upload">Carica</Button>

      <p v-if="localError" role="alert" class="text-sm text-danger">{{ localError }}</p>
      <p v-if="attach.isError.value" role="alert" class="text-sm text-danger">
        {{ attach.error.value?.message }}
      </p>
      <p v-if="detach.isError.value" role="alert" class="text-sm text-danger">
        {{ detach.error.value?.message }}
      </p>
      <p v-if="setCover.isError.value" role="alert" class="text-sm text-danger">
        {{ setCover.error.value?.message }}
      </p>
    </div>
  </Card>
</template>
