/// <reference types="vite/client" />

/**
 * Makes `.vue` single-file components importable from TypeScript.
 * Required because TypeScript itself does not understand the `.vue` extension.
 */
declare module "*.vue" {
  import type { DefineComponent } from "vue";

  const component: DefineComponent<Record<string, unknown>, Record<string, unknown>, unknown>;
  export default component;
}
