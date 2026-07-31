/**
 * Shared Prettier config for the whole monorepo.
 * Formatting is centralized here; ESLint never handles formatting (see eslint.config.mjs).
 */
export default {
  semi: true,
  singleQuote: false,
  printWidth: 100,
  trailingComma: "all",
  endOfLine: "lf",
};
