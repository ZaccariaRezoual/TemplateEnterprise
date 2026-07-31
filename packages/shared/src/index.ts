/**
 * Public surface of `@enterprise/shared`.
 * Everything importable from this package is re-exported here.
 */
export {
  ApplicationError,
  BusinessError,
  ForbiddenError,
  NetworkError,
  NotFoundError,
  ServerError,
  UnauthorizedError,
  ValidationError,
} from "./errors/applicationError";
export { mapResponseToApplicationError, mapTransportFailure } from "./api/errorMapper";
export { parseProblemDetails, problemDetailsSchema } from "./api/problemDetails";
export type { ProblemDetails } from "./api/problemDetails";
export { executeSdkCall, unwrapSdkResult } from "./api/unwrap";
export type { SdkResult } from "./api/unwrap";
export { isDefined, isNonEmptyString } from "./utils/guards";
export { DEFAULT_LOCALE, formatCurrency, formatDate, formatNumber } from "./utils/format";
export {
  emailSchema,
  entityIdSchema,
  pageRequestSchema,
  passwordSchema,
  type PageRequestInput,
} from "./validators/common";
