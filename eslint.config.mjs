/**
 * Shared ESLint flat config for the whole monorepo.
 *
 * Responsibilities:
 * - Base JS + strict TypeScript rules for every workspace package.
 * - Vue 3 recommended rules for `.vue` single-file components.
 * - Prettier compatibility (formatting is owned by Prettier, never by ESLint).
 *
 * Individual packages may extend this config but must not weaken it.
 */
import js from "@eslint/js";
import prettier from "eslint-config-prettier";
import vue from "eslint-plugin-vue";
import tseslint from "typescript-eslint";
import vueParser from "vue-eslint-parser";

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
  ...vue.configs["flat/recommended"],
  {
    files: ["**/*.vue"],
    languageOptions: {
      parser: vueParser,
      parserOptions: {
        // The <script> block is TypeScript; only the template is parsed by vue-eslint-parser.
        parser: tseslint.parser,
        ecmaVersion: "latest",
        sourceType: "module",
      },
    },
    rules: {
      // Single-word component names are fine for pages and layouts, which are
      // never used as tags in templates.
      "vue/multi-word-component-names": "off",
    },
  },
  prettier,
);
