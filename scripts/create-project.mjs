#!/usr/bin/env node
/**
 * Creates a new project from this template.
 *
 * The framework is a starting point, not a dependency: a project gets its own
 * copy of the code and owns it from day one. This script performs the
 * mechanical part of that copy — renaming the identifiers that would otherwise
 * stay "EnterpriseFramework" forever.
 *
 * It deliberately does NOT remove the Demo module. The demo is wired into the
 * router, the navigation and the realtime e2e test (it owns the only control
 * that triggers a server-sent notification), so removing it mechanically would
 * hand over a repository whose test suite no longer passes. Removing it is a
 * short manual step, documented in docs/create-project.md.
 *
 * Only files tracked by git are copied, which is what keeps node_modules,
 * bin/, obj/ and local settings out of the new repository without maintaining
 * an ignore list here that would drift from .gitignore.
 *
 * Usage:
 *   node scripts/create-project.mjs --name "Acme CRM" --target ../acme-crm
 *   node scripts/create-project.mjs --name "Acme CRM" --target ../acme-crm --scope @acme
 *   node scripts/create-project.mjs --name "Acme CRM" --target ../acme-crm --dry-run
 *
 * Options:
 *   --name       Human-readable product name. Everything else is derived from it.
 *   --target     Directory to create. Must not exist, or must be empty.
 *   --scope      npm scope for the workspace packages. Default: derived from --name.
 *   --dry-run    Report what would happen without writing anything.
 */
import { execFileSync } from "node:child_process";
import { existsSync, mkdirSync, readdirSync, readFileSync, writeFileSync } from "node:fs";
import { dirname, join, relative, resolve, sep } from "node:path";
import { fileURLToPath } from "node:url";

const repoRoot = resolve(dirname(fileURLToPath(import.meta.url)), "..");

/** Identifiers of the template, replaced everywhere they occur. */
const TEMPLATE = {
  /** .NET root namespace and assembly prefix. */
  namespace: "EnterpriseFramework",
  /** npm workspace scope. */
  scope: "@enterprise",
  /** Docker Compose project and container prefix. */
  slug: "enterprise-framework",
  /** Product name shown in the UI. */
  product: "Enterprise Framework",
};

/** Text file extensions whose contents are rewritten. Anything else is copied verbatim. */
const TEXT_EXTENSIONS = new Set([
  ".cs",
  ".csproj",
  ".sln",
  ".props",
  ".targets",
  ".json",
  ".jsonc",
  ".ts",
  ".mts",
  ".mjs",
  ".js",
  ".vue",
  ".css",
  ".html",
  ".md",
  ".yml",
  ".yaml",
  ".editorconfig",
  ".gitignore",
  ".gitattributes",
  ".env",
  ".example",
  ".sql",
  ".ps1",
  ".sh",
  ".resx",
  ".xml",
  ".config",
]);

function parseArguments(argv) {
  const options = { dryRun: false };

  for (let index = 0; index < argv.length; index += 1) {
    const argument = argv[index];
    switch (argument) {
      case "--name":
        options.name = argv[++index];
        break;
      case "--target":
        options.target = argv[++index];
        break;
      case "--scope":
        options.scope = argv[++index];
        break;
      case "--dry-run":
        options.dryRun = true;
        break;
      default:
        fail(`Unknown option: ${argument}`);
    }
  }

  if (!options.name) fail('--name is required, e.g. --name "Acme CRM"');
  if (!options.target) fail("--target is required, e.g. --target ../acme-crm");
  return options;
}

function fail(message) {
  console.error(`create-project: ${message}`);
  process.exit(1);
}

/** "Acme CRM" -> "acme-crm". The npm scope and the Docker project name. */
function toSlug(name) {
  return name
    .normalize("NFKD")
    .replace(/[^\w\s-]/g, "")
    .trim()
    .replace(/[\s_]+/g, "-")
    .toLowerCase();
}

/** "Acme CRM" -> "AcmeCrm". The .NET root namespace: it must be a valid identifier. */
function toNamespace(name) {
  const parts = name
    .normalize("NFKD")
    .replace(/[^\w\s-]/g, " ")
    .split(/[\s_-]+/)
    .filter(Boolean)
    .map((part) => part[0].toUpperCase() + part.slice(1).toLowerCase());
  const identifier = parts.join("");

  if (identifier.length === 0) fail(`Cannot derive a namespace from "${name}"`);
  // A namespace may not start with a digit; prefixing keeps the build valid
  // rather than failing at the first compile in the new repository.
  return /^\d/.test(identifier) ? `P${identifier}` : identifier;
}

/** Every occurrence of a template identifier, replaced by the project's. */
function rewrite(content, target) {
  return content
    .replaceAll(TEMPLATE.namespace, target.namespace)
    .replaceAll(`${TEMPLATE.scope}/`, `${target.scope}/`)
    .replaceAll(TEMPLATE.product, target.product)
    .replaceAll(TEMPLATE.slug, target.slug);
}

function isTextFile(path) {
  const name = path.slice(path.lastIndexOf("/") + 1);
  const dot = name.lastIndexOf(".");
  if (dot <= 0) return true; // extensionless dotfiles and LICENSE-style files
  return TEXT_EXTENSIONS.has(name.slice(dot));
}

const options = parseArguments(process.argv.slice(2));
const target = {
  product: options.name,
  namespace: toNamespace(options.name),
  slug: toSlug(options.name),
  scope: options.scope ?? `@${toSlug(options.name)}`,
};
const targetRoot = resolve(process.cwd(), options.target);

if (existsSync(targetRoot) && readdirSync(targetRoot).length > 0) {
  fail(`${targetRoot} exists and is not empty`);
}

// Tracked files only: the working tree may hold build output and local
// settings, and none of that belongs in a fresh repository.
const files = execFileSync("git", ["ls-files", "-z"], { cwd: repoRoot, encoding: "utf8" })
  .split("\0")
  .filter(Boolean);

console.log(`Product   : ${target.product}`);
console.log(`Namespace : ${target.namespace}`);
console.log(`npm scope : ${target.scope}`);
console.log(`Docker    : ${target.slug}`);
console.log(`Target    : ${targetRoot}`);
console.log("");

let copied = 0;
let skipped = 0;

for (const file of files) {
  // The template's own scaffolder does not belong in the generated project:
  // a project is created once, and shipping the script invites a second run
  // inside a repository that is no longer the template.
  if (file === "scripts/create-project.mjs") {
    skipped += 1;
    continue;
  }

  const destination = join(targetRoot, rewrite(file, target).split("/").join(sep));

  if (options.dryRun) {
    copied += 1;
    continue;
  }

  mkdirSync(dirname(destination), { recursive: true });

  if (isTextFile(file)) {
    writeFileSync(destination, rewrite(readFileSync(join(repoRoot, file), "utf8"), target));
  } else {
    writeFileSync(destination, readFileSync(join(repoRoot, file)));
  }

  copied += 1;
}

console.log(
  options.dryRun
    ? `Would copy ${copied} files (${skipped} skipped).`
    : `Copied ${copied} files (${skipped} skipped).`,
);

if (options.dryRun) process.exit(0);

console.log(`
Next steps in ${relative(process.cwd(), targetRoot) || "."}:

  git init && git add -A && git commit -m "chore: initial commit from template"
  pnpm install
  dotnet build
  docker compose -f docker/docker-compose.yml up -d
  pnpm --filter ${target.scope}/web dev

Then make it yours:
  1. packages/ui/src/styles/themes/  - your brand's semantic token overrides
  2. docs/design-system.md           - document what you changed
  3. modules/                        - your business modules; the framework ones stay
  4. docs/create-project.md          - how to remove the Demo module when you no
                                       longer need it as a reference
`);
