/**
 * Public surface of `@enterprise/module-services`.
 *
 * The host consumes four things: `installServicesModule` at bootstrap,
 * `servicesAdminRoutes` and `servicesPublicRoutes` in its route registry, and
 * `serviceImageUrl` if it ever needs to render a service image outside this
 * module.
 */
export { installServicesModule, applyRuntimeSeo } from "./install";
export type { RuntimeSeo, ServicesModuleHost, SetSeo } from "./install";
export { servicesAdminRoutes, servicesPublicRoutes } from "./routes";
export {
  servicesApi,
  serviceImageUrl,
  provideServicesApi,
  type AdminService,
  type PublicService,
  type ServiceImage,
  type ServiceWriteModel,
} from "./api/services.api";
export {
  servicesKeys,
  useArchiveService,
  usePublicService,
  usePublicServices,
  useSaveService,
  useService,
  useServiceImages,
  useServicesList,
} from "./composables/useServices";
export { serviceFormSchema } from "./validators/service.validator";
export type { ServiceFormValues } from "./validators/service.validator";
