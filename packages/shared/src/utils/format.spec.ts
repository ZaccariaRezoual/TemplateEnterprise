import { describe, expect, it } from "vitest";
import { formatCurrency, formatDate, formatNumber } from "./format";

describe("formatNumber", () => {
  it("uses the requested locale rather than the ambient one", () => {
    // Five digits, not four: Italian CLDR sets minimumGroupingDigits=2, so
    // "1234" is deliberately left ungrouped while "12345" becomes "12.345".
    expect(formatNumber(12345.5, { locale: "it-IT" })).toBe("12.345,5");
    expect(formatNumber(12345.5, { locale: "en-US" })).toBe("12,345.5");
  });

  it("caps the decimals when asked", () => {
    expect(formatNumber(1.23456, { locale: "en-US", maximumFractionDigits: 2 })).toBe("1.23");
  });
});

describe("formatCurrency", () => {
  it("includes the currency for the given locale", () => {
    expect(formatCurrency(9.9, "EUR", "it-IT")).toContain("9,90");
    expect(formatCurrency(9.9, "USD", "en-US")).toBe("$9.90");
  });
});

describe("formatDate", () => {
  it("formats an ISO string coming from the API", () => {
    expect(formatDate("2026-07-31T10:00:00Z", { locale: "en-US" })).toBe("Jul 31, 2026");
  });

  it("accepts a Date instance", () => {
    expect(formatDate(new Date("2026-01-02T00:00:00Z"), { locale: "en-US" })).toBe("Jan 2, 2026");
  });

  it("returns an empty string for an unparsable value instead of 'Invalid Date'", () => {
    expect(formatDate("not-a-date")).toBe("");
  });
});
