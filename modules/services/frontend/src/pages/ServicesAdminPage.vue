<script setup lang="ts">
/**
 * -----------------------------------------------------------------------------
 * ServicesAdminPage
 * -----------------------------------------------------------------------------
 *
 * Administration list of the catalogue.
 *
 * Responsibilities:
 * - Reads the whole catalogue through the module's composable.
 * - Filters and sorts it in the browser, and lets the administrator archive
 *   an entry.
 * - Handles the three states a data screen must always handle — loading,
 *   error and empty — and tells "no services yet" apart from "nothing matches
 *   this search", which are different facts.
 *
 * The business logic lives in the composables; this component decides what is
 * on screen. Filtering client-side is deliberate: the endpoint returns the
 * whole catalogue because a catalogue is a handful of entries by design, so a
 * search that costs a round trip would be slower and no more correct.
 */
import { Badge, Button, Card, Checkbox, Input } from "@enterprise/ui";
import { usePermissions } from "@enterprise/module-authorization";
import { computed, ref } from "vue";
import { RouterLink } from "vue-router";
import type { AdminService } from "../api/services.api";
import { useArchiveService, useServicesList } from "../composables/useServices";

const { can } = usePermissions();

const services = useServicesList();
const archive = useArchiveService();

const search = ref("");
const showArchived = ref(false);

/** Which service the confirmation row is open for, if any. */
const pendingArchive = ref<string | undefined>(undefined);

const visible = computed<AdminService[]>(() => {
  const term = search.value.trim().toLowerCase();
  const all = services.data.value ?? [];

  return all.filter((service) => {
    if (!showArchived.value && service.isArchived) {
      return false;
    }
    if (term === "") {
      return true;
    }
    return service.title.toLowerCase().includes(term) || service.slug.toLowerCase().includes(term);
  });
});

/** True when the catalogue is empty, as opposed to filtered down to nothing. */
const isCatalogueEmpty = computed(() => (services.data.value ?? []).length === 0);

/** The state to show as a badge: three mutually exclusive facts, in order. */
function statusOf(service: AdminService): { label: string; variant: "neutral" | "success" } {
  if (service.isArchived) {
    return { label: "Archiviato", variant: "neutral" };
  }
  return service.isPublished
    ? { label: "Pubblicato", variant: "success" }
    : { label: "Bozza", variant: "neutral" };
}

/** Duration and price, joined only when both are published. */
function detailsOf(service: AdminService): string {
  const parts: string[] = [];

  if (service.durationMinutes !== null && service.durationMinutes !== undefined) {
    parts.push(`${service.durationMinutes} min`);
  }
  if (service.price !== null && service.price !== undefined) {
    parts.push(`${service.price} ${service.currency ?? ""}`.trim());
  }

  return parts.join(" · ");
}

async function confirmArchive(id: string): Promise<void> {
  await archive.mutateAsync(id);
  pendingArchive.value = undefined;
}
</script>

<template>
  <div class="space-y-6">
    <header class="flex flex-wrap items-start justify-between gap-4">
      <div>
        <h1 class="text-display font-semibold tracking-tight">Servizi</h1>
        <p class="mt-1 text-text-muted">
          Il catalogo pubblicato sulla vetrina, più le bozze non ancora visibili.
        </p>
      </div>

      <!-- Rendered only for who can act on it: an action that answers 403 is
           worse than an action that is not there. An anchor driven by the
           router, so middle-click and "open in new tab" keep working. -->
      <RouterLink
        v-if="can('services.write')"
        v-slot="{ href, navigate }"
        :to="{ name: 'services-admin-new' }"
        custom
      >
        <Button :href="href" @click="navigate">Nuovo servizio</Button>
      </RouterLink>
    </header>

    <p v-if="!can('services.read')" role="alert" class="text-sm text-danger">
      Non hai il permesso di vedere i servizi.
    </p>

    <template v-else>
      <div class="flex flex-col gap-3 sm:flex-row sm:items-center">
        <Input
          v-model="search"
          label="Cerca"
          placeholder="Titolo o indirizzo…"
          label-hidden
          type="search"
        />
        <Checkbox v-model="showArchived" label="Mostra archiviati" class="sm:w-auto sm:shrink-0" />
      </div>

      <Card flush>
        <p v-if="services.isPending.value" class="p-5 text-sm text-text-muted">Caricamento…</p>

        <p v-else-if="services.isError.value" role="alert" class="p-5 text-sm text-danger">
          {{ services.error.value?.message }}
        </p>

        <p v-else-if="isCatalogueEmpty" class="p-8 text-center text-text-muted">
          Nessun servizio: creane uno per vederlo comparire in vetrina.
        </p>

        <p v-else-if="visible.length === 0" class="p-8 text-center text-text-muted">
          Nessun servizio corrisponde alla ricerca.
        </p>

        <!-- A data table keeps all its columns and scrolls WITHIN this region
             on narrow screens. `min-w` is what makes that work: without it
             `w-full` compresses the columns into unreadable slivers instead of
             overflowing. `tabindex` makes the region keyboard-scrollable,
             which overflow containers otherwise are not. -->
        <div
          v-else
          class="overflow-x-auto"
          tabindex="0"
          role="region"
          aria-label="Catalogo dei servizi, scorrevole"
        >
          <table class="w-full min-w-2xl text-left text-sm">
            <caption class="sr-only">
              Catalogo dei servizi
            </caption>
            <thead class="border-b border-border text-text-muted">
              <tr>
                <th scope="col" class="px-5 py-3 font-medium">Titolo</th>
                <th scope="col" class="px-5 py-3 font-medium">Indirizzo</th>
                <th scope="col" class="px-5 py-3 font-medium">Stato</th>
                <th scope="col" class="px-5 py-3 font-medium">Dati</th>
                <th scope="col" class="px-5 py-3 font-medium">Ordine</th>
                <th scope="col" class="px-5 py-3 text-right font-medium">Azioni</th>
              </tr>
            </thead>
            <tbody>
              <tr
                v-for="service in visible"
                :key="service.id"
                class="border-b border-border last:border-0"
              >
                <td class="px-5 py-3 font-medium">{{ service.title }}</td>
                <td class="truncate px-5 py-3 text-text-muted">/{{ service.slug }}</td>
                <td class="px-5 py-3">
                  <Badge :variant="statusOf(service).variant">{{ statusOf(service).label }}</Badge>
                  <!-- A second badge, because "published" and "bookable" are
                       different facts and colour alone cannot say both. -->
                  <Badge v-if="service.isBookable" variant="info" class="ml-1.5">Prenotabile</Badge>
                </td>
                <td class="px-5 py-3 text-text-muted tabular-nums">{{ detailsOf(service) }}</td>
                <td class="px-5 py-3 text-text-muted tabular-nums">{{ service.sortOrder }}</td>
                <td class="px-5 py-3">
                  <div class="flex items-center justify-end gap-2">
                    <RouterLink
                      v-if="can('services.write') && !service.isArchived"
                      :to="{ name: 'services-admin-edit', params: { id: service.id } }"
                      class="text-sm font-medium text-primary underline-offset-2 hover:underline"
                    >
                      Modifica
                    </RouterLink>

                    <template v-if="can('services.delete') && !service.isArchived">
                      <Button
                        v-if="pendingArchive !== service.id"
                        variant="ghost"
                        size="sm"
                        @click="pendingArchive = service.id"
                      >
                        Archivia
                      </Button>

                      <!-- Inline confirmation rather than a dialog: archiving
                           is reversible only by creating a new service, so it
                           deserves a second step — but a modal for a
                           one-click action in a table is heavier than the
                           action itself. -->
                      <template v-else>
                        <Button
                          variant="danger"
                          size="sm"
                          :loading="archive.isPending.value"
                          @click="confirmArchive(service.id)"
                        >
                          Confermi?
                        </Button>
                        <Button variant="ghost" size="sm" @click="pendingArchive = undefined">
                          Annulla
                        </Button>
                      </template>
                    </template>
                  </div>
                </td>
              </tr>
            </tbody>
          </table>
        </div>
      </Card>

      <p v-if="archive.isError.value" role="alert" class="text-sm text-danger">
        {{ archive.error.value?.message }}
      </p>
    </template>
  </div>
</template>
