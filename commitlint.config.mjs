/**
 * Commitlint config: enforces Conventional Commits on every commit message
 * (wired to the commit-msg hook via Husky, see .husky/commit-msg).
 */
export default {
  extends: ["@commitlint/config-conventional"],
};
