import { z } from "zod";

/**
 * Validation schemas reused across features and modules.
 *
 * Defining them once keeps client-side rules consistent with the backend
 * FluentValidation rules; a feature composes these instead of re-declaring
 * "what a valid email is" in five places.
 */

/** Email address, normalized to lowercase and trimmed. */
export const emailSchema = z.string().trim().toLowerCase().email("Enter a valid email address.");

/**
 * Password policy shared by sign-up and password change.
 * Mirrors the backend policy; the server remains the authority.
 */
export const passwordSchema = z
  .string()
  .min(12, "Use at least 12 characters.")
  .regex(/[a-z]/, "Include a lowercase letter.")
  .regex(/[A-Z]/, "Include an uppercase letter.")
  .regex(/\d/, "Include a digit.");

/** Identifier of a persisted entity, as emitted by the API. */
export const entityIdSchema = z.string().uuid("Not a valid identifier.");

/** Query parameters accepted by paginated endpoints. */
export const pageRequestSchema = z.object({
  page: z.coerce.number().int().positive().default(1),
  pageSize: z.coerce.number().int().positive().max(200).default(25),
  sortBy: z.string().optional(),
  sortDirection: z.enum(["asc", "desc"]).optional(),
});

/** Parsed pagination parameters, with defaults applied. */
export type PageRequestInput = z.infer<typeof pageRequestSchema>;
