import { z } from "zod";

/**
 * Client-side rules for the echo form, mirroring the backend
 * `EchoCommandValidator`.
 *
 * Client validation is for fast feedback only: the server remains the
 * authority and its 400 responses are surfaced as `ValidationError`.
 */
export const echoFormSchema = z.object({
  text: z.string().min(1, "Enter some text to echo.").max(500, "Maximum 500 characters."),
});

/** Values handled by the echo form. */
export type EchoFormValues = z.infer<typeof echoFormSchema>;
