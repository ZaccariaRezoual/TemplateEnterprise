import { describe, expect, it } from "vitest";
import { parseEnv } from "@/core/config/env";

describe("parseEnv", () => {
  it("applies defaults when optional variables are absent", () => {
    const parsed = parseEnv({});

    expect(parsed.VITE_API_BASE_URL).toBe("/api");
    expect(parsed.VITE_API_TIMEOUT_MS).toBe(15_000);
    expect(parsed.VITE_LOG_LEVEL).toBe("info");
  });

  it("coerces the timeout coming from the environment as a string", () => {
    const parsed = parseEnv({ VITE_API_TIMEOUT_MS: "2500" });

    expect(parsed.VITE_API_TIMEOUT_MS).toBe(2500);
  });

  it("fails fast, naming the offending key, on an invalid value", () => {
    expect(() => parseEnv({ VITE_LOG_LEVEL: "verbose" })).toThrowError(/VITE_LOG_LEVEL/);
  });

  it("rejects a non-positive timeout", () => {
    expect(() => parseEnv({ VITE_API_TIMEOUT_MS: "0" })).toThrowError(/VITE_API_TIMEOUT_MS/);
  });
});
