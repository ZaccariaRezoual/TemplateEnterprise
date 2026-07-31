import { describe, expect, it } from "vitest";
import { emailSchema, pageRequestSchema, passwordSchema } from "./common";

describe("emailSchema", () => {
  it("normalizes casing and surrounding whitespace", () => {
    expect(emailSchema.parse("  User@Example.COM ")).toBe("user@example.com");
  });

  it("rejects a malformed address", () => {
    expect(emailSchema.safeParse("not-an-email").success).toBe(false);
  });
});

describe("passwordSchema", () => {
  it("accepts a password meeting every rule", () => {
    expect(passwordSchema.safeParse("Str0ngPassphrase").success).toBe(true);
  });

  it.each([
    ["Short1A", "too short"],
    ["alllowercase1", "no uppercase"],
    ["ALLUPPERCASE1", "no lowercase"],
    ["NoDigitsHereAtAll", "no digit"],
  ])("rejects %s (%s)", (password) => {
    expect(passwordSchema.safeParse(password).success).toBe(false);
  });
});

describe("pageRequestSchema", () => {
  it("applies defaults when nothing is provided", () => {
    expect(pageRequestSchema.parse({})).toEqual({ page: 1, pageSize: 25 });
  });

  it("coerces query-string values, which are always strings", () => {
    expect(pageRequestSchema.parse({ page: "3", pageSize: "50" })).toMatchObject({
      page: 3,
      pageSize: 50,
    });
  });

  it("caps the page size to protect the API", () => {
    expect(pageRequestSchema.safeParse({ pageSize: 5000 }).success).toBe(false);
  });
});
