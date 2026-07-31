import { emailSchema, passwordSchema } from "@enterprise/shared";
import { z } from "zod";

/**
 * Client-side rules for the auth forms, mirroring the backend validators
 * (fast feedback only — the server remains the authority). Email and password
 * rules come from @enterprise/shared so every module agrees on them.
 */

/** Rules of the sign-in form. Password is presence-only: policy is not enforced at login. */
export const loginFormSchema = z.object({
  email: emailSchema,
  password: z.string().min(1, "Enter your password."),
});

/** Values handled by the sign-in form. */
export type LoginFormValues = z.infer<typeof loginFormSchema>;

/** Rules of the registration form. */
export const registerFormSchema = z.object({
  email: emailSchema,
  displayName: z
    .string()
    .trim()
    .min(1, "Enter a display name.")
    .max(200, "Maximum 200 characters."),
  password: passwordSchema,
});

/** Values handled by the registration form. */
export type RegisterFormValues = z.infer<typeof registerFormSchema>;
