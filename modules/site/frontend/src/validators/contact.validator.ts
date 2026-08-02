import { emailSchema } from "@enterprise/shared";
import { z } from "zod";

/**
 * Client-side rules of the contact form, mirroring the backend validator.
 *
 * Fast feedback only: the server remains the authority and revalidates
 * everything — a visitor can reach the endpoint without ever loading this
 * page.
 */
export const contactFormSchema = z.object({
  name: z.string().trim().min(1, "Inserisci il tuo nome.").max(200, "Massimo 200 caratteri."),
  email: emailSchema,
  body: z.string().trim().min(1, "Scrivi il tuo messaggio.").max(5000, "Massimo 5000 caratteri."),
});

/** Values handled by the contact form. */
export type ContactFormValues = z.infer<typeof contactFormSchema>;
