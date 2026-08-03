import { z } from "zod";

/**
 * Client-side rules of the service form, mirroring the backend validator.
 *
 * Fast feedback only: the server remains the authority and revalidates
 * everything. The two conditional rules are the ones worth mirroring, because
 * they are the ones a form cannot express structurally — a bookable service
 * needs a duration, and a price needs a currency.
 */
export const serviceFormSchema = z
  .object({
    title: z.string().trim().min(1, "Il titolo è obbligatorio.").max(200, "Massimo 200 caratteri."),
    slug: z
      .string()
      .trim()
      .max(200, "Massimo 200 caratteri.")
      .refine(
        (value) => value === "" || /^[a-z0-9]+(?:-[a-z0-9]+)*$/.test(value),
        "Solo lettere minuscole, cifre e trattini singoli.",
      ),
    shortDescription: z
      .string()
      .trim()
      .min(1, "Serve una riga di presentazione: è quella che si legge in elenco.")
      .max(300, "Massimo 300 caratteri."),
    description: z.string().trim().max(8000, "Massimo 8000 caratteri."),
    durationMinutes: z
      .number()
      .int("Indica i minuti come numero intero.")
      .positive("La durata deve essere maggiore di zero.")
      .max(24 * 60, "Al massimo 24 ore.")
      .nullable(),
    price: z.number().min(0, "Il prezzo non può essere negativo.").nullable(),
    currency: z.string().trim().nullable(),
    isPublished: z.boolean(),
    isBookable: z.boolean(),
    sortOrder: z.number().int().min(0, "L'ordine non può essere negativo."),
  })
  .refine((model) => !model.isBookable || model.durationMinutes !== null, {
    path: ["durationMinutes"],
    message: "Un servizio prenotabile ha bisogno di una durata: senza, non ci sono slot.",
  })
  .refine((model) => model.price === null || (model.currency ?? "").length === 3, {
    path: ["currency"],
    message: "Un prezzo ha bisogno di una valuta ISO 4217, per esempio EUR.",
  });

/** Values handled by the service form. */
export type ServiceFormValues = z.infer<typeof serviceFormSchema>;
