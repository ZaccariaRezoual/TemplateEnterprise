import type { ApiClient, components } from "@enterprise/sdk";
import { executeSdkCall } from "@enterprise/shared";

/** A service as the administration sees it: every field, drafts included. */
export type AdminService = components["schemas"]["AdminServiceDto"];

/** A service as an anonymous visitor sees it. */
export type PublicService = components["schemas"]["PublicServiceDto"];

/** One picture of a service. */
export type ServiceImage = components["schemas"]["ServiceImageDto"];

/** The editable fields of a service, as the form submits them. */
export type ServiceWriteModel = components["schemas"]["ServiceWriteModel"];

/** Metadata of a file uploaded to the Storage module. */
export type StoredFile = components["schemas"]["StoredFileDto"];

let configuredApi: ApiClient | undefined;

/**
 * Origin of the API, used to build the public URL of an image.
 *
 * Empty for a same-origin deployment, which is the normal case: the SDK's
 * paths already carry the full route.
 */
let apiOrigin = "";

/**
 * Injects the application's SDK client and API origin. Called once by
 * `installServicesModule`.
 *
 * @param api The configured SDK client.
 * @param origin Origin of the API, or an empty string when same-origin.
 */
export function provideServicesApi(api: ApiClient, origin: string): void {
  configuredApi = api;
  apiOrigin = origin;
}

function requireApi(): ApiClient {
  if (configuredApi === undefined) {
    throw new Error("Services module is not installed. Call installServicesModule() at bootstrap.");
  }
  return configuredApi;
}

/**
 * Builds the URL an `<img>` loads a service image from.
 *
 * It points at the Storage module's PUBLIC route, which needs no token: the
 * showcase is read by visitors who have no session. The image must therefore
 * have been uploaded as public — {@link servicesApi.uploadImageFile} is the
 * only upload path this module uses, and it always is.
 *
 * @param storageFileId Identifier of the file in the Storage module.
 * @returns The absolute or root-relative URL of the image.
 */
export function serviceImageUrl(storageFileId: string): string {
  return `${apiOrigin}/api/files/public/${storageFileId}`;
}

/**
 * Feature service of the Services module: the only place that knows which API
 * operations it uses. Components go through composables, never here.
 */
export const servicesApi = {
  /**
   * Lists the published services for the showcase.
   *
   * @param signal Abort signal forwarded by TanStack Query on cancellation.
   * @returns The published services, in showcase order.
   */
  listPublic(signal?: AbortSignal): Promise<PublicService[]> {
    return executeSdkCall(() =>
      requireApi().GET("/api/services", { ...(signal === undefined ? {} : { signal }) }),
    );
  },

  /**
   * Reads one published service by its public address.
   *
   * @param slug URL segment of the page being opened.
   * @param signal Abort signal forwarded by TanStack Query on cancellation.
   * @returns The service.
   * @throws {import("@enterprise/shared").NotFoundError} When no VISIBLE service carries that slug.
   */
  getPublic(slug: string, signal?: AbortSignal): Promise<PublicService> {
    return executeSdkCall(() =>
      requireApi().GET("/api/services/{slug}", {
        params: { path: { slug } },
        ...(signal === undefined ? {} : { signal }),
      }),
    );
  },

  /**
   * Lists the whole catalogue for the administration.
   *
   * @param signal Abort signal forwarded by TanStack Query on cancellation.
   * @returns Every service, drafts and archived entries included.
   * @throws {import("@enterprise/shared").ForbiddenError} Without the services.read permission.
   */
  list(signal?: AbortSignal): Promise<AdminService[]> {
    return executeSdkCall(() =>
      requireApi().GET("/api/admin/services", { ...(signal === undefined ? {} : { signal }) }),
    );
  },

  /**
   * Reads one service in any state, for the edit form.
   *
   * @param id Identifier of the service.
   * @param signal Abort signal forwarded by TanStack Query on cancellation.
   * @returns The service.
   * @throws {import("@enterprise/shared").ForbiddenError} Without the services.read permission.
   */
  get(id: string, signal?: AbortSignal): Promise<AdminService> {
    return executeSdkCall(() =>
      requireApi().GET("/api/admin/services/{id}", {
        params: { path: { id } },
        ...(signal === undefined ? {} : { signal }),
      }),
    );
  },

  /**
   * Adds a service to the catalogue.
   *
   * @param model The editable fields.
   * @returns The created service.
   * @throws {import("@enterprise/shared").ValidationError} When a field is rejected.
   * @throws {import("@enterprise/shared").BusinessError} When the slug is already taken.
   */
  create(model: ServiceWriteModel): Promise<AdminService> {
    return executeSdkCall(() => requireApi().POST("/api/admin/services", { body: model }));
  },

  /**
   * Edits a service of the catalogue.
   *
   * @param id Identifier of the service.
   * @param model The editable fields.
   * @returns The updated service.
   * @throws {import("@enterprise/shared").ValidationError} When a field is rejected.
   * @throws {import("@enterprise/shared").BusinessError} When the service is archived or the slug is taken.
   */
  update(id: string, model: ServiceWriteModel): Promise<AdminService> {
    return executeSdkCall(() =>
      requireApi().PUT("/api/admin/services/{id}", {
        params: { path: { id } },
        body: model,
      }),
    );
  },

  /**
   * Withdraws a service from the catalogue. There is no delete.
   *
   * @param id Identifier of the service.
   * @throws {import("@enterprise/shared").ForbiddenError} Without the services.delete permission.
   */
  archive(id: string): Promise<void> {
    return executeSdkCall(() =>
      requireApi().POST("/api/admin/services/{id}/archive", { params: { path: { id } } }),
    );
  },

  /**
   * Uploads an image file to the Storage module, marked PUBLIC.
   *
   * Public is not a choice offered to the caller: an image of the showcase
   * has to load for a visitor with no session, and a private one would show
   * as a broken picture with no error anywhere.
   *
   * @param file The picture chosen in the form.
   * @returns The stored file metadata, whose id links it to a service.
   * @throws {import("@enterprise/shared").ForbiddenError} Without the files.write permission.
   */
  uploadImageFile(file: File): Promise<StoredFile> {
    const form = new FormData();
    form.append("file", file);
    form.append("visibility", "Public");

    return executeSdkCall(() =>
      requireApi().POST("/api/files", {
        // The generated types model a multipart body as an object of strings
        // — OpenAPI has no File type — so the FormData goes through the
        // default serializer, which forwards it untouched and lets the
        // browser set the multipart boundary.
        body: form as unknown as { file: string },
      }),
    );
  },

  /**
   * Attaches an uploaded file to a service as one of its images.
   *
   * @param serviceId Service the image belongs to.
   * @param image The file identifier, its text alternative and its position.
   * @returns The attached image.
   * @throws {import("@enterprise/shared").ValidationError} When the alt text is missing.
   */
  attachImage(
    serviceId: string,
    image: { storageFileId: string; altText: string; sortOrder: number },
  ): Promise<ServiceImage> {
    return executeSdkCall(() =>
      requireApi().POST("/api/admin/services/{id}/images", {
        params: { path: { id: serviceId } },
        body: image,
      }),
    );
  },

  /**
   * Chooses the image shown on cards and in link previews.
   *
   * @param serviceId Service the image belongs to.
   * @param imageId Image to promote.
   */
  setCover(serviceId: string, imageId: string): Promise<void> {
    return executeSdkCall(() =>
      requireApi().PUT("/api/admin/services/{id}/images/{imageId}/cover", {
        params: { path: { id: serviceId, imageId } },
      }),
    );
  },

  /**
   * Detaches an image from a service. The file itself is left to Storage.
   *
   * @param serviceId Service the image belongs to.
   * @param imageId Image to detach.
   */
  detachImage(serviceId: string, imageId: string): Promise<void> {
    return executeSdkCall(() =>
      requireApi().DELETE("/api/admin/services/{id}/images/{imageId}", {
        params: { path: { id: serviceId, imageId } },
      }),
    );
  },
};
