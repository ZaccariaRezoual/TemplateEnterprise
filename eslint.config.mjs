/**
 * Shared ESLint flat config for the whole monorepo.
 *
 * Responsibilities:
 * - Base JS + strict TypeScript rules for every workspace package.
 * - Vue 3 recommended rules for `.vue` single-file components.
 * - Declares which globals exist where: application and package source runs in
 *   the browser, build scripts and tooling config run in Node. Getting this
 *   wrong is what makes `no-undef` fire on `document` or `process`.
 * - Prettier compatibility (formatting is owned by Prettier, never by ESLint).
 *
 * Individual packages may extend this config but must not weaken it.
 */
import js from "@eslint/js";
import prettier from "eslint-config-prettier";
import vue from "eslint-plugin-vue";
import globals from "globals";
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
      "**/storybook-static/",
      // Generated from the OpenAPI document; regenerate rather than edit.
      "packages/sdk/src/generated/",
    ],
  },
  js.configs.recommended,
  ...tseslint.configs.recommended,
  ...vue.configs["flat/recommended"],

  {
    rules: {
      // Underscore-prefixed bindings are intentionally discarded — the common
      // case being `const { class: _class, ...rest } = attrs` when a component
      // merges the consumer's class itself instead of letting it fall through.
      "@typescript-eslint/no-unused-vars": [
        "error",
        {
          argsIgnorePattern: "^_",
          varsIgnorePattern: "^_",
          caughtErrorsIgnorePattern: "^_",
          ignoreRestSiblings: true,
        },
      ],
    },
  },

  // Browser code: applications and packages.
  {
    files: ["apps/**/src/**", "packages/**/src/**"],
    languageOptions: { globals: globals.browser },
  },

  // Node code: build scripts, tooling config and end-to-end tests.
  {
    files: [
      // Both the repository's scripts and an app's own build scripts: the
      // prerender step lives in apps/web because it needs that app's tooling.
      "**/scripts/**",
      "**/*.config.{js,mjs,ts}",
      "**/.storybook/**",
      "**/e2e/**",
      "**/vitest.setup.ts",
    ],
    languageOptions: { globals: globals.node },
  },

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
