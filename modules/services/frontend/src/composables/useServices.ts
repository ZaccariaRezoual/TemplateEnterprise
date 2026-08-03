import { useMutation, useQuery, useQueryClient } from "@tanstack/vue-query";
import { computed, type Ref } from "vue";
import {
  servicesApi,
  type AdminService,
  type PublicService,
  type ServiceWriteModel,
} from "../api/services.api";

/**
 * Query keys of the Services module.
 *
 * Centralized so cache invalidation never relies on a hand-typed string, and
 * so the split between the administrative and public caches is visible: they
 * are different data with different visibility rules, and one must never
 * satisfy a request for the other.
 */
export const servicesKeys = {
  all: ["services"] as const,
  adminList: () => [...servicesKeys.all, "admin", "list"] as const,
  adminDetail: (id: string) => [...servicesKeys.all, "admin", "detail", id] as const,
  publicList: () => [...servicesKeys.all, "public", "list"] as const,
  publicDetail: (slug: string) => [...servicesKeys.all, "public", "detail", slug] as const,
};

/**
 * Reads the whole catalogue for the administration.
 *
 * Server state, so TanStack Query owns it. The list is unpaged by design —
 * see the backend query — which is what makes searching and sorting a client
 * concern in the page.
 *
 * @returns The query result: `data`, `isPending`, `isError`, `error`.
 */
export function useServicesList() {
  return useQuery<AdminService[]>({
    queryKey: servicesKeys.adminList(),
    queryFn: ({ signal }) => servicesApi.list(signal),
  });
}

/**
 * Reads one service for the edit form.
 *
 * @param id Reactive identifier; the query is disabled while it is empty,
 * which is the "create" case of the same form.
 * @returns The query result.
 */
export function useService(id: Ref<string>) {
  return useQuery<AdminService>({
    queryKey: computed(() => servicesKeys.adminDetail(id.value)),
    queryFn: ({ signal }) => servicesApi.get(id.value, signal),
    enabled: computed(() => id.value !== ""),
  });
}

/**
 * Creates and edits services.
 *
 * After a successful write it invalidates BOTH caches: the administrative
 * list, and the public one — publishing a service must change the showcase
 * for whoever is looking at it in another tab of the same session, and the
 * page that publishes has no way to know who is.
 *
 * @returns `save` (create or update), plus `isPending` and `error`.
 */
export function useSaveService() {
  const queryClient = useQueryClient();

  const mutation = useMutation({
    mutationFn: (input: { id?: string | undefined; model: ServiceWriteModel }) =>
      input.id === undefined || input.id === ""
        ? servicesApi.create(input.model)
        : servicesApi.update(input.id, input.model),
    onSuccess: (service) => {
      void queryClient.invalidateQueries({ queryKey: servicesKeys.all });
      // The detail cache is seeded rather than invalidated: the response IS
      // the new state, so refetching it would be a round trip for data we
      // already hold.
      queryClient.setQueryData(servicesKeys.adminDetail(service.id), service);
    },
  });

  return mutation;
}

/**
 * Withdraws a service from the catalogue.
 *
 * @returns The mutation: `mutateAsync`, `isPending`, `error`.
 */
export function useArchiveService() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (id: string) => servicesApi.archive(id),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: servicesKeys.all }),
  });
}

/**
 * Manages the gallery of a service: upload, cover and detach.
 *
 * Uploading is TWO calls — the file to Storage, then the association to this
 * module — and they are bundled here so no page has to know that, or get the
 * order wrong.
 *
 * @param serviceId Reactive identifier of the service being edited.
 * @returns `attach`, `setCover` and `detach` mutations.
 */
export function useServiceImages(serviceId: Ref<string>) {
  const queryClient = useQueryClient();

  const refresh = () => {
    void queryClient.invalidateQueries({ queryKey: servicesKeys.all });
  };

  const attach = useMutation({
    mutationFn: async (input: { file: File; altText: string; sortOrder: number }) => {
      const stored = await servicesApi.uploadImageFile(input.file);
      return servicesApi.attachImage(serviceId.value, {
        storageFileId: stored.id,
        altText: input.altText,
        sortOrder: input.sortOrder,
      });
    },
    onSuccess: refresh,
  });

  const setCover = useMutation({
    mutationFn: (imageId: string) => servicesApi.setCover(serviceId.value, imageId),
    onSuccess: refresh,
  });

  const detach = useMutation({
    mutationFn: (imageId: string) => servicesApi.detachImage(serviceId.value, imageId),
    onSuccess: refresh,
  });

  return { attach, setCover, detach };
}

/**
 * Reads the published catalogue for the showcase.
 *
 * @returns The query result.
 */
export function usePublicServices() {
  return useQuery<PublicService[]>({
    queryKey: servicesKeys.publicList(),
    queryFn: ({ signal }) => servicesApi.listPublic(signal),
  });
}

/**
 * Reads one published service by its address.
 *
 * @param slug Reactive URL segment of the page being opened.
 * @returns The query result.
 */
export function usePublicService(slug: Ref<string>) {
  return useQuery<PublicService>({
    queryKey: computed(() => servicesKeys.publicDetail(slug.value)),
    queryFn: ({ signal }) => servicesApi.getPublic(slug.value, signal),
    enabled: computed(() => slug.value !== ""),
    // A 404 here means the address does not exist, and retrying will not
    // invent it. Retrying would only delay the "not found" the visitor
    // needs to see.
    retry: false,
  });
}
