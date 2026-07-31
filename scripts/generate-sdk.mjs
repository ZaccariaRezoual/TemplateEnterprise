#!/usr/bin/env node
/**
 * Regenerates the typed SDK from the API contract.
 *
 * Pipeline: `dotnet build` writes apps/api/openapi/v1.json (build-time
 * generation, the API never has to run) → openapi-typescript turns it into
 * packages/sdk/src/generated/schema.ts.
 *
 * Run it after changing an endpoint or a contract, and commit both outputs:
 * the OpenAPI document makes API changes reviewable in the pull request diff,
 * and CI verifies the pair is in sync (see `--check`).
 *
 * Usage:
 *   node scripts/generate-sdk.mjs            regenerate
 *   node scripts/generate-sdk.mjs --check    fail if the result would differ
 */
import { execFileSync } from "node:child_process";
import { readFileSync } from "node:fs";
import { dirname, join } from "node:path";
import { fileURLToPath } from "node:url";

const repoRoot = join(dirname(fileURLToPath(import.meta.url)), "..");
const apiProject = join(repoRoot, "apps/api/src/Api");
const documentPath = join(repoRoot, "apps/api/openapi/v1.json");
const schemaPath = join(repoRoot, "packages/sdk/src/generated/schema.ts");
const checkOnly = process.argv.includes("--check");

function run(command, args) {
  execFileSync(command, args, { cwd: repoRoot, stdio: "inherit", shell: true });
}

function readOrEmpty(path) {
  try {
    return readFileSync(path, "utf8");
  } catch {
    return "";
  }
}

const previousDocument = readOrEmpty(documentPath);
const previousSchema = readOrEmpty(schemaPath);

console.log("Building the API to refresh the OpenAPI document…");
run("dotnet", ["build", apiProject, "--nologo", "--verbosity", "quiet"]);

console.log("Generating the typed SDK…");
run("pnpm", ["--filter", "@enterprise/sdk", "generate"]);

if (!checkOnly) {
  console.log("SDK is up to date.");
  process.exit(0);
}

const documentChanged = readOrEmpty(documentPath) !== previousDocument;
const schemaChanged = readOrEmpty(schemaPath) !== previousSchema;

if (documentChanged || schemaChanged) {
  console.error(
    "\nThe committed SDK is stale. Run `node scripts/generate-sdk.mjs` and commit:\n" +
      (documentChanged ? "  - apps/api/openapi/v1.json\n" : "") +
      (schemaChanged ? "  - packages/sdk/src/generated/schema.ts\n" : ""),
  );
  process.exit(1);
}

console.log("SDK matches the API contract.");
