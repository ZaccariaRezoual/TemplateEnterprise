import { z } from "zod";

/**
 * RFC 9457 ProblemDetails payload as produced by the API's
 * `GlobalExceptionHandler`, including the framework extensions
 * (`errors` for validation failures, `correlationId` for support).
 */
export const problemDetailsSchema = z.object({
  title: z.string().optional(),
  detail: z.string().optional(),
  status: z.number().optional(),
  errors: z.record(z.string(), z.array(z.string())).optional(),
  correlationId: z.string().optional(),
});

/** Parsed ProblemDetails payload. */
export type ProblemDetails = z.infer<typeof problemDetailsSchema>;

/**
 * Parses an unknown response body as ProblemDetails.
 *
 * @param body Raw response body.
 * @returns The parsed payload, or `undefined` when the body is not
 * ProblemDetails (e.g. an HTML error page from a proxy).
 */
export function parseProblemDetails(body: unknown): ProblemDetails | undefined {
  const result = problemDetailsSchema.safeParse(body);
  return result.success ? result.data : undefined;
}
