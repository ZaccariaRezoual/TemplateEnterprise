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
export default defineConfig({
  plugins: [vue(), tailwindcss()],
  resolve: {
    alias: {
      "@": fileURLToPath(new URL("./src", import.meta.url)),
    },
  },
  server: {
    port: 5173,
    proxy: {
      "/api": {
        target: process.env["VITE_DEV_API_TARGET"] ?? "http://localhost:5080",
        changeOrigin: true,
      },
    },
  },
});
