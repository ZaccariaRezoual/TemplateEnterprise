/**
 * Shared ESLint flat config for the whole monorepo.
 *
 * Responsibilities:
 * - Base JS + strict TypeScript rules for every workspace package.
 * - Prettier compatibility (formatting is owned by Prettier, never by ESLint).
 *
 * Vue-specific rules (eslint-plugin-vue) are added in Fase 2 together with apps/web.
 * Individual packages may extend this config but must not weaken it.
 */
import js from "@eslint/js";
import prettier from "eslint-config-prettier";
import tseslint from "typescript-eslint";

export default tseslint.config(
  {
    ignores: [
      "**/node_modules/",
      "**/dist/",
      "**/coverage/",
      "**/bin/",
      "**/obj/",
      "**/playwright-report/",
      "**/test-results/",
    ],
  },
  js.configs.recommended,
  ...tseslint.configs.recommended,
  prettier,
);
