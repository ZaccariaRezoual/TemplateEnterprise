import { fileURLToPath, URL } from "node:url";
import tailwindcss from "@tailwindcss/vite";
import vue from "@vitejs/plugin-vue";
import { defineConfig } from "vite";

/**
 * Vite configuration for the web application.
 *
 * The dev server proxies "/api" to the backend so the browser always calls a
 * same-origin URL: no CORS in development and the production deployment
 * (reverse proxy in front of both) behaves identically.
 */
/** Where the dev server forwards API and hub traffic. */
const apiTarget = process.env["VITE_DEV_API_TARGET"] ?? "http://localhost:5080";

const apiProxy = {
  "/api": {
    target: apiTarget,
    changeOrigin: true,
  },
  // The SignalR hub. `ws: true` is required: without it the negotiate
  // request is proxied but the WebSocket upgrade is not, and the client
  // silently falls back to long polling — or fails outright.
  "/hubs": {
    target: apiTarget,
    changeOrigin: true,
    ws: true,
  },
};

export default defineConfig({
  plugins: [vue(), tailwindcss()],
  resolve: {
    alias: {
      "@": fileURLToPath(new URL("./src", import.meta.url)),
    },
  },
  server: {
    port: 5173,
    proxy: apiProxy,
  },
  // The preview server proxies too, and that is not a convenience: the
  // prerender (`scripts/prerender.mjs`) drives a headless browser against it,
  // and a public page that reads data — the services showcase does — would
  // otherwise be captured showing its error state.
  preview: {
    proxy: apiProxy,
  },
});
