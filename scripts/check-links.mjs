#!/usr/bin/env node
/**
 * Verifies that every relative link in the Markdown documentation points at a
 * file that exists.
 *
 * Broken links are the first symptom of documentation drifting away from the
 * code — a file gets renamed, the prose keeps pointing at where it used to be,
 * and the page silently becomes a dead end. It is also the cheapest kind of rot
 * to catch, which is why it runs in CI.
 *
 * Only relative links are checked. External URLs would need network access and
 * would make the build fail for reasons that have nothing to do with the
 * change under review.
 *
 * Usage:
 *   node scripts/check-links.mjs            check the documented roots
 *   node scripts/check-links.mjs wiki docs  check specific roots
 */
import { readdirSync, readFileSync, existsSync, statSync } from "node:fs";
import { dirname, join, resolve, relative } from "node:path";
import { fileURLToPath } from "node:url";

const repoRoot = resolve(dirname(fileURLToPath(import.meta.url)), "..");

/** Documentation roots scanned when no argument is given. */
const DEFAULT_ROOTS = ["wiki", "docs", "modules", "plans"];

/** Repository-root documents, which are entry points and link out everywhere. */
const ROOT_FILES = ["README.md", "CLAUDE.md", "PLAN.md", "Struttura.md"];

/** Directories never worth walking into. */
const SKIP = new Set(["node_modules", "bin", "obj", "dist", ".git", "coverage"]);

/** `[text](target)`, ignoring images and reference-style definitions. */
const LINK = /\[[^\]]*\]\(([^)\s]+)(?:\s+"[^"]*")?\)/g;

function markdownFiles(directory) {
  const found = [];

  for (const entry of readdirSync(directory, { withFileTypes: true })) {
    if (SKIP.has(entry.name)) continue;
    const path = join(directory, entry.name);

    if (entry.isDirectory()) found.push(...markdownFiles(path));
    else if (entry.name.endsWith(".md")) found.push(path);
  }

  return found;
}

const explicit = process.argv.slice(2);
const roots = explicit.length > 0 ? explicit : [...DEFAULT_ROOTS, ...ROOT_FILES];
const broken = [];
let checked = 0;

for (const root of roots) {
  const absoluteRoot = join(repoRoot, root);
  if (!existsSync(absoluteRoot)) continue;

  const files = statSync(absoluteRoot).isDirectory() ? markdownFiles(absoluteRoot) : [absoluteRoot];

  for (const file of files) {
    const content = readFileSync(file, "utf8");

    for (const [, rawTarget] of content.matchAll(LINK)) {
      // Anchors and external links are out of scope: one needs a heading
      // parser, the other needs the network.
      if (/^(https?:|mailto:|#)/.test(rawTarget)) continue;

      const target = decodeURI(rawTarget.split("#")[0]);
      if (target === "") continue;

      checked += 1;
      const resolved = resolve(dirname(file), target);
      // A link to a directory is valid: GitHub renders its README.
      if (!existsSync(resolved) && !existsSync(join(resolved, "README.md"))) {
        broken.push(`${relative(repoRoot, file)} → ${rawTarget}`);
      }
    }
  }
}

if (broken.length > 0) {
  console.error(`Broken links (${broken.length}):\n`);
  for (const entry of broken) console.error(`  ${entry}`);
  console.error("");
  process.exit(1);
}

console.log(`Checked ${checked} relative links in ${roots.join(", ")}: all resolve.`);
