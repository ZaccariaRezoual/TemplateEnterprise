import { describe, expect, it } from "vitest";
import { serviceFormSchema, type ServiceFormValues } from "./service.validator";

/**
 * The client-side rules of the service form.
 *
 * Only the conditional ones are worth testing: nothing about two independent
 * booleans and a nullable number says that one implies the other, and the
 * server rejecting the combination later is a worse way to find out.
 */
function values(overrides: Partial<ServiceFormValues> = {}): unknown {
  return {
    title: "Consulenza",
    slug: "",
    shortDescription: "Un'ora per mettere a fuoco il problema.",
    description: "",
    durationMinutes: null,
    price: null,
    currency: null,
    isPublished: false,
    isBookable: false,
    sortOrder: 0,
    ...overrides,
  };
}

describe("service form rules", () => {
  it("refuses a bookable service with no duration", () => {
    const result = serviceFormSchema.safeParse(values({ isBookable: true, durationMinutes: null }));

    expect(result.success).toBe(false);
    expect(result.error?.issues.some((issue) => issue.path[0] === "durationMinutes")).toBe(true);
  });

  it("accepts a bookable service with a duration", () => {
    expect(
      serviceFormSchema.safeParse(values({ isBookable: true, durationMinutes: 60 })).success,
    ).toBe(true);
  });

  it("accepts a service that is only described", () => {
    expect(
      serviceFormSchema.safeParse(values({ isBookable: false, durationMinutes: null })).success,
    ).toBe(true);
  });

  it("refuses a price with no currency", () => {
    const result = serviceFormSchema.safeParse(values({ price: 120, currency: null }));

    expect(result.success).toBe(false);
    expect(result.error?.issues.some((issue) => issue.path[0] === "currency")).toBe(true);
  });

  it("accepts publishing no price at all", () => {
    expect(serviceFormSchema.safeParse(values({ price: null, currency: null })).success).toBe(true);
  });

  it("accepts an empty slug, which means 'derive it from the title'", () => {
    expect(serviceFormSchema.safeParse(values({ slug: "" })).success).toBe(true);
  });

  it.each(["Consulenza", "con sulenza", "consulenza--doppio", "consulenza-"])(
    "refuses the slug %s, which is not already normalized",
    (slug) => {
      // A typed slug is used verbatim by the server, so the form must refuse
      // anything the normalizer would have changed — otherwise what is stored
      // differs from what the administrator read back.
      expect(serviceFormSchema.safeParse(values({ slug })).success).toBe(false);
    },
  );
});
